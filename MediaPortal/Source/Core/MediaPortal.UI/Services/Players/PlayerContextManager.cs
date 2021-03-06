#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Players
{
  /// <summary>
  /// Implementation of the <see cref="IPlayerContextManager"/> interface.
  /// </summary>
  public class PlayerContextManager : IPlayerContextManager, IDisposable
  {
    #region Enums & classes

    protected enum PlayerWFStateType
    {
      CurrentlyPlaying,
      FullscreenContent
    }

    /// <summary>
    /// Stores a descriptor for an instance of a "currently playing" or "fullscreen content" workflow navigation state
    /// on the workflow navigation context stack.
    /// </summary>
    protected class PlayerWFStateInstance
    {
      protected PlayerWFStateType _type;
      protected Guid _wfStateId;

      public PlayerWFStateInstance(PlayerWFStateType type, Guid wfStateId)
      {
        _type = type;
        _wfStateId = wfStateId;
      }

      public PlayerWFStateType WFStateType
      {
        get { return _type; }
      }

      public Guid WFStateId
      {
        get { return _wfStateId; }
      }
    }

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    private int _currentPlayerIndex = -1; // Set this value via the CurrentPlayerIndex property to correctly raise the update event

    /// <summary>
    /// Remembers all player workflow state instances ("currently playing" or "fullscreen content" states) from the current
    /// workflow navigation stack. We need that to update those states when the current player or primary player change.
    /// The topmost player workflow state instance from the navigation stack is at the end of this list.
    /// </summary>
    /// <remarks>
    /// Note that we can be in a CP state and in an FSC state at the same time.
    /// </remarks>
    protected List<PlayerWFStateInstance> _playerWfStateInstances = new List<PlayerWFStateInstance>(2);

    #endregion

    public PlayerContextManager()
    {
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           WorkflowManagerMessaging.CHANNEL,
           PlayerManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        // Adjust our knowledge about the currently opened FSC/CP state if the user switches to one of them
        WorkflowManagerMessaging.MessageType messageType =
            (WorkflowManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WorkflowManagerMessaging.MessageType.StatesPopped:
            ICollection<Guid> statesRemoved = new List<Guid>(
                ((IDictionary<Guid, NavigationContext>) message.MessageData[WorkflowManagerMessaging.CONTEXTS]).Keys);
            lock (SyncObj)
            {
              // If one of our remembered player workflow states was removed from workflow navigation stack,
              // take it from our player workflow state cache. Our player workflow state cache is in the same order
              // as the workflow navigation stack, so if we tracked everything correctly, each removal of states should
              // remove states from the end of our cache.
              for (int i = 0; i < _playerWfStateInstances.Count; i++)
              {
                PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
                if (statesRemoved.Contains(wfStateInstance.WFStateId))
                {
                  _playerWfStateInstances.RemoveRange(i, _playerWfStateInstances.Count - i);
                  break;
                }
              }
            }
            break;
          case WorkflowManagerMessaging.MessageType.StatePushed:
            NavigationContext context = (NavigationContext) message.MessageData[WorkflowManagerMessaging.CONTEXT];
            Guid stateId = context.WorkflowState.StateId;
            Guid? potentialState = GetPotentialCPStateId();
            if (potentialState == stateId)
              lock(SyncObj)
                _playerWfStateInstances.Add(new PlayerWFStateInstance(PlayerWFStateType.CurrentlyPlaying, stateId));
            potentialState = GetPotentialFSCStateId();
            if (potentialState == stateId)
              lock (SyncObj)
                _playerWfStateInstances.Add(new PlayerWFStateInstance(PlayerWFStateType.FullscreenContent, stateId));
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        // React to player changes
        PlayerManagerMessaging.MessageType messageType =
            (PlayerManagerMessaging.MessageType) message.MessageType;
        PlayerContext pc;
        IPlayerSlotController psc;
        uint activationSequence;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            activationSequence = (uint) message.MessageData[PlayerManagerMessaging.ACTIVATION_SEQUENCE];
            pc = PlayerContext.GetPlayerContext(psc);
            if (pc == null || !pc.IsValid || psc.ActivationSequence != activationSequence)
              return;
            if (!pc.NextItem())
            {
              if (pc.CloseWhenFinished)
                pc.Close();
              else
                psc.Stop();
              if (psc.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT)
                StepOutOfPlayerWFState(PlayerWFStateType.FullscreenContent);
              if (psc.SlotIndex == CurrentPlayerIndex)
                StepOutOfPlayerWFState(PlayerWFStateType.CurrentlyPlaying);
            }
            break;
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            activationSequence = (uint) message.MessageData[PlayerManagerMessaging.ACTIVATION_SEQUENCE];
            pc = PlayerContext.GetPlayerContext(psc);
            if (pc == null || !pc.IsValid || psc.ActivationSequence != activationSequence)
              return;
            // We get the player message asynchronously, so we have to check the state of the slot again to ensure
            // we close the correct one
            if (pc.CloseWhenFinished && pc.CurrentPlayer == null)
              pc.Close();
            if (psc.SlotIndex == PlayerManagerConsts.PRIMARY_SLOT)
              StepOutOfPlayerWFState(PlayerWFStateType.FullscreenContent);
            if (psc.SlotIndex == CurrentPlayerIndex)
              StepOutOfPlayerWFState(PlayerWFStateType.CurrentlyPlaying);
            break;
          case PlayerManagerMessaging.MessageType.RequestNextItem:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            activationSequence = (uint) message.MessageData[PlayerManagerMessaging.ACTIVATION_SEQUENCE];
            pc = PlayerContext.GetPlayerContext(psc);
            if (pc == null || !pc.IsValid || psc.ActivationSequence != activationSequence)
              return;
            pc.RequestNextItem_NoLock();
            break;
          case PlayerManagerMessaging.MessageType.PlayerSlotsChanged:
            lock (SyncObj)
              CurrentPlayerIndex = 1 - _currentPlayerIndex;
            break;
        }
        CheckCurrentPlayerSlot(); // Current player could have been closed
        CheckMediaWorkflowStates_NoLock(); // Primary player could have been changed or closed or CP player could have been closed
      }
    }

    /// <summary>
    /// Checks if the current player context contains a current player and returns the player. Else returns
    /// <c>null</c>.
    /// </summary>
    /// <returns>Current player of the current player context, if present. Else <c>null</c>.</returns>
    protected IPlayer GetCurrentPlayer()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return null;
      return playerContext.CurrentPlayer;
    }

    /// <summary>
    /// Sets the audio signal to the most plausible player. This is the audio player, if present, else it is the
    /// primary video player, if present.
    /// </summary>
    protected static void CheckAudio()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (playerManager.AudioSlotIndex != -1)
          return;
        IPlayer primaryPlayer = playerManager[PlayerManagerConsts.PRIMARY_SLOT];
        IPlayer secondaryPlayer = playerManager[PlayerManagerConsts.SECONDARY_SLOT];
        if (primaryPlayer is IAudioPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        else if (secondaryPlayer is IAudioPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
        else if (primaryPlayer is IVideoPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        else if (secondaryPlayer is IVideoPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
        else
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
      }
    }

    protected void CheckCurrentPlayerSlot()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerSlotController primaryPSC = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayerSlotController secondaryPSC = playerManager.GetPlayerSlotController(PlayerManagerConsts.SECONDARY_SLOT);
      lock (playerManager.SyncObj)
      {
        bool primaryPlayerActive = primaryPSC.IsActive;
        bool secondaryPlayerActive = secondaryPSC.IsActive;
        int currentPlayerIndex = _currentPlayerIndex;
        if (currentPlayerIndex == PlayerManagerConsts.PRIMARY_SLOT && !primaryPlayerActive)
          currentPlayerIndex = -1;
        else if (currentPlayerIndex == PlayerManagerConsts.SECONDARY_SLOT && !secondaryPlayerActive)
          currentPlayerIndex = -1;
        if (currentPlayerIndex == -1)
          if (primaryPlayerActive)
            currentPlayerIndex = PlayerManagerConsts.PRIMARY_SLOT;
        CurrentPlayerIndex = currentPlayerIndex;
      }
    }

    /// <summary>
    /// Schedules an asynchronous call to <see cref="CheckMediaWorkflowStates_NoLock"/> at the global thread pool.
    /// </summary>
    protected void CheckMediaWorkflowStates_Async()
    {
      ServiceRegistration.Get<IThreadPool>().Add(new DoWorkHandler(CheckMediaWorkflowStates_NoLock), "PlayerContextManager: CheckMediaWorkflowStates");
    }

    /// <summary>
    /// Gets the "currently playing" workflow state for the current player.
    /// </summary>
    /// <returns></returns>
    protected Guid? GetPotentialCPStateId()
    {
      IPlayerContext currentPC = CurrentPlayerContext;
      return currentPC == null ? new Guid?() : currentPC.CurrentlyPlayingWorkflowStateId;
    }

    /// <summary>
    /// Gets the "fullscreen content" workflow state for the primary player.
    /// </summary>
    /// <returns></returns>
    protected Guid? GetPotentialFSCStateId()
    {
      IPlayerContext currentPC = GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      return currentPC == null ? new Guid?() : currentPC.FullscreenContentWorkflowStateId;
    }

    /// <summary>
    /// Check if one of our tracked workflow states has the given <paramref name="type"/>. If yes, go out of that state.
    /// </summary>
    protected void StepOutOfPlayerWFState(PlayerWFStateType type)
    {
      for (int i = 0; i < _playerWfStateInstances.Count; i++)
      {
        PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
        if (wfStateInstance.WFStateType == type)
        { // Found FSC state - step out of it
          ServiceRegistration.Get<ILogger>().Debug("PlayerContextManager: Leaving {0} Workflow State '{1}'", type, wfStateInstance.WFStateId);
          lock (SyncObj)
            // Remove all workflow states until the removed player workflow state
            _playerWfStateInstances.RemoveRange(i, _playerWfStateInstances.Count - i);
          ServiceRegistration.Get<IWorkflowManager>().NavigatePopToStateAsync(wfStateInstance.WFStateId, true);
          return;
        }
      }
    }

    /// <summary>
    /// Checks if our "currently playing" and "fullscreen content" states still fit to the
    /// appropriate players, i.e. if we are in a "currently playing" state and the current player context was
    /// changed, the workflow state will be adapted to match the new current player context's "currently playing" state.
    /// The same check will happen for the primary player context and the "fullscreen content" state.
    /// </summary>
    /// <remarks>
    /// This method must not be called when the player manager's lock is held.
    /// </remarks>
    protected void CheckMediaWorkflowStates_NoLock()
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState != SystemState.Running)
        // Only automatically change workflow states in running state
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdateAsync();
      ILogger log = ServiceRegistration.Get<ILogger>();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      try
      {
        for (int i = 0; i < _playerWfStateInstances.Count; i++)
        {
          // Find the first workflow state of our cached player workflow states which doesn't fit any more
          // and update to the new player workflow state of the same player workflow state type, if necessary.
          PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
          Guid? newStateId;
          string stateName;
          switch (wfStateInstance.WFStateType)
          {
            case PlayerWFStateType.CurrentlyPlaying:
              newStateId = GetPotentialCPStateId();
              stateName = "Currently Playing";
              break;
            case PlayerWFStateType.FullscreenContent:
              newStateId = GetPotentialFSCStateId();
              stateName = "Fullscreen Content";
              break;
            default:
              throw new NotImplementedException(string.Format("No handler for player workflow state type '{0}'",
                  wfStateInstance.WFStateType));
          }
          if (newStateId != wfStateInstance.WFStateId)
          {
            // Found the first player workflow state which doesn't fit any more
            log.Debug("PlayerContextManager: {0} Workflow State '{1}' doesn't fit any more to the current situation. Leaving workflow state...",
                stateName, wfStateInstance.WFStateId);
            lock (playerManager.SyncObj)
              // Remove all workflow states until the player workflow state which doesn't fit any more
              _playerWfStateInstances.RemoveRange(i, _playerWfStateInstances.Count - i);
            workflowManager.NavigatePopToStateAsync(wfStateInstance.WFStateId, true);
            if (newStateId.HasValue)
            {
              log.Debug("PlayerContextManager: ... Auto-switching to new '{0}' Workflow State '{1}'",
                  stateName, newStateId.Value);
              workflowManager.NavigatePushAsync(newStateId.Value);
            }
            break;
          }
        }
      }
      finally
      {
        workflowManager.EndBatchUpdateAsync();
      }
    }

    protected int FindIndexOfPlayerWFStateType(PlayerWFStateType type)
    {
      lock (SyncObj)
        for (int i = 0; i < _playerWfStateInstances.Count; i++)
        {
          PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
          if (wfStateInstance.WFStateType == type)
            return i;
        }
      return -1;
    }

    protected static PlayerContext GetPlayerContextInternal(int slotIndex)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      return PlayerContext.GetPlayerContext(playerManager.GetPlayerSlotController(slotIndex));
    }

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion

    #region IPlayerContextManager implementation

    // Returns the player manager's synchronization object; depending on if we already have a local reference to the
    // player manager, we will request the sync object's reference on the player manager itself or we use this property.
    public object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    public bool IsAudioPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        // We don't need to lock here because of the ||
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IAudioPlayer ||
            playerManager[PlayerManagerConsts.SECONDARY_SLOT] is IAudioPlayer;
      }
    }

    public bool IsVideoPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        // No locking necessary
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IVideoPlayer;
      }
    }

    public bool IsPipActive
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        // No locking necessary
        IPlayer player = playerManager[PlayerManagerConsts.SECONDARY_SLOT];
        return player is IVideoPlayer || player is IImagePlayer;
      }
    }

    public bool IsCurrentlyPlayingWorkflowStateActive
    {
      get { return FindIndexOfPlayerWFStateType(PlayerWFStateType.CurrentlyPlaying) != -1; }
    }

    public bool IsFullscreenContentWorkflowStateActive
    {
      get { return FindIndexOfPlayerWFStateType(PlayerWFStateType.FullscreenContent) != -1; }
    }

    public IPlayerContext CurrentPlayerContext
    {
      get { return GetPlayerContextInternal(_currentPlayerIndex); }
    }

    public int CurrentPlayerIndex
    {
      get
      {
        lock (SyncObj)
          return _currentPlayerIndex;
      }
      set
      {
        lock (SyncObj)
        {
          if (_currentPlayerIndex == value)
            return;
          if (value != PlayerManagerConsts.PRIMARY_SLOT && value != PlayerManagerConsts.SECONDARY_SLOT && value != -1)
            return;
          _currentPlayerIndex = value;
          CheckCurrentPlayerSlot();
          CheckMediaWorkflowStates_Async();
          PlayerContextManagerMessaging.SendPlayerContextManagerMessage(
              PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged, value);
        }
      }
    }

    public int NumActivePlayerContexts
    {
      get
      {
        lock (SyncObj)
        {
          int result = 0;
          for (int i = 0; i < 2; i++)
            if (GetPlayerContext(i) != null)
              result++;
          return result;
        }
      }
    }

    public void Shutdown()
    {
      UnsubscribeFromMessages();
    }

    public int NumPlayerContextsOfType(AVType avType)
    {
      lock (SyncObj)
      {
        int result = 0;
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext pc = GetPlayerContext(i);
          if (pc != null && pc.AVType == avType)
            result++;
        }
        return result;
      }
    }

    public IPlayerContext OpenAudioPlayerContext(Guid mediaModuleId, string name, bool concurrentVideo, Guid currentlyPlayingWorkflowStateId,
        Guid fullscreenContentWorkflowStateId)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int numActive = playerManager.NumActiveSlots;
        if (concurrentVideo)
        {
          // Solve conflicts - close conflicting slots
          if (numActive > 1)
            playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
          IPlayerContext pc;
          if (numActive > 0 && (pc = GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT)) != null && pc.AVType == AVType.Audio)
            playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
        }
        else // !concurrentVideo
          // Don't enable concurrent controllers: Close all except the primary slot controller
          playerManager.CloseAllSlots();
        // Open new slot
        int slotIndex;
        IPlayerSlotController slotController;
        playerManager.OpenSlot(out slotIndex, out slotController);
        playerManager.AudioSlotIndex = slotController.SlotIndex;
        return new PlayerContext(this, slotController, mediaModuleId, name, AVType.Audio,
            currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
      }
    }

    public IPlayerContext OpenVideoPlayerContext(Guid mediaModuleId, string name, PlayerContextConcurrencyMode concurrencyMode,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        IList<IPlayerContext> playerContexts = new List<IPlayerContext>(2);
        IPlayerContext pc = GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
        if (pc != null)
        {
          playerContexts.Add(pc);
          pc = GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT);
          if (pc != null)
            playerContexts.Add(pc);
        }
        int numActive = playerContexts.Count;
        IPlayerSlotController slotController;
        int slotIndex;
        switch (concurrencyMode)
        {
          case PlayerContextConcurrencyMode.ConcurrentAudio:
            if (numActive > 1 && playerContexts[1].AVType == AVType.Audio)
            { // The secondary slot is an audio player slot
              playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.SwitchSlots();
              playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            }
            else if (numActive == 1 && playerContexts[0].AVType == AVType.Audio)
            { // The primary slot is an audio player slot
              playerManager.OpenSlot(out slotIndex, out slotController);
              // Make new video slot the primary slot
              playerManager.SwitchSlots();
              playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            }
            else
            { // No audio slot available
              playerManager.CloseAllSlots();
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            break;
          case PlayerContextConcurrencyMode.ConcurrentVideo:
            if (numActive >= 1 && playerContexts[0].AVType == AVType.Video)
            { // The primary slot is a video player slot
              if (numActive > 1)
                playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            else
            {
              playerManager.CloseAllSlots();
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
            break;
          default:
            // Don't enable concurrent controllers: Close all except the primary slot controller
            playerManager.CloseAllSlots();
            playerManager.OpenSlot(out slotIndex, out slotController);
            playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            break;
        }
        return new PlayerContext(this, slotController, mediaModuleId, name, AVType.Video,
            currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
      }
    }

    public IEnumerable<IPlayerContext> GetPlayerContextsByMediaModuleId(Guid mediaModuleId)
    {
      lock (SyncObj)
      {
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext pc = GetPlayerContext(i);
          if (pc != null && pc.MediaModuleId == mediaModuleId)
            yield return pc;
        }
      }
    }

    public IEnumerable<IPlayerContext> GetPlayerContextsByAVType(AVType avType)
    {
      lock (SyncObj)
      {
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext pc = GetPlayerContext(i);
          if (pc != null && pc.AVType == avType)
            yield return pc;
        }
      }
    }

    public void ShowCurrentlyPlaying(bool asynchronously)
    {
      Guid currentlyPlayingStateId;
      lock (SyncObj)
      {
        if (IsCurrentlyPlayingWorkflowStateActive)
          return;
        IPlayerContext pc = GetPlayerContext(_currentPlayerIndex);
        if (pc == null)
          return;
        currentlyPlayingStateId = pc.CurrentlyPlayingWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (asynchronously)
        workflowManager.NavigatePushAsync(currentlyPlayingStateId);
      else
        workflowManager.NavigatePush(currentlyPlayingStateId);
      // TODO: Would be good if we could wait for the CP screen to be shown. But to achieve that, we need the ScreenManager to send
      // a message when a screen change has completed.
    }

    public void ShowFullscreenContent(bool asynchronously)
    {
      Guid fullscreenContentStateId;
      lock (SyncObj)
      {
        if (IsFullscreenContentWorkflowStateActive)
          return;
        IPlayerContext pc = GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
        if (pc == null)
          return;
        fullscreenContentStateId = pc.FullscreenContentWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (asynchronously)
        workflowManager.NavigatePushAsync(fullscreenContentStateId);
      else
        workflowManager.NavigatePush(fullscreenContentStateId);
      // TODO: Would be good if we could wait for the FSC screen to be shown. But to achieve that, we need the ScreenManager to send
      // a message when a screen change has completed.
    }

    public AVType GetTypeOfMediaItem(MediaItem item)
    {
      // No locking necessary
      if (item.Aspects.ContainsKey(VideoAspect.Metadata.AspectId) ||
          item.Aspects.ContainsKey(ImageAspect.Metadata.AspectId))
        return AVType.Video;
      if (item.Aspects.ContainsKey(AudioAspect.Metadata.AspectId))
        return AVType.Audio;
      return AVType.None;
    }

    public IPlayerContext GetPlayerContext(PlayerChoice player)
    {
      lock (SyncObj)
      {
        int slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        switch (player)
        {
          case PlayerChoice.PrimaryPlayer:
            slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            break;
          case PlayerChoice.SecondaryPlayer:
            slotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            break;
          case PlayerChoice.CurrentPlayer:
            slotIndex = _currentPlayerIndex;
            break;
          case PlayerChoice.NotCurrentPlayer:
            slotIndex = 1 - _currentPlayerIndex;
            break;
        }
        return GetPlayerContextInternal(slotIndex);
      }
    }

    public IPlayerContext GetPlayerContext(int slotIndex)
    {
      return GetPlayerContextInternal(slotIndex);
    }

    public ICollection<AudioStreamDescriptor> GetAvailableAudioStreams(out AudioStreamDescriptor currentAudioStream)
    {
      currentAudioStream = null;
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      ICollection<AudioStreamDescriptor> result = new List<AudioStreamDescriptor>();
      int audioSlotIndex = playerManager.AudioSlotIndex;
      for (int i = 0; i < 2; i++)
      {
        IPlayerContext playerContext = GetPlayerContext(i);
        if (playerContext == null)
          continue;
        AudioStreamDescriptor current;
        CollectionUtils.AddAll(result, playerContext.GetAudioStreamDescriptors(out current));
        if (audioSlotIndex == i)
          currentAudioStream = current;
      }
      return result;
    }

    public bool SetAudioStream(AudioStreamDescriptor stream)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerContext playerContext = stream.PlayerContext;
      lock (playerManager.SyncObj)
      {
        if (!playerContext.IsValid)
          return false;
        IPlayer player = playerContext.CurrentPlayer;
        if (player == null || player.Name != stream.PlayerName)
          return false;
        IVideoPlayer videoPlayer = player as IVideoPlayer;
        if (videoPlayer != null)
          videoPlayer.SetAudioStream(stream.AudioStreamName);
        playerManager.AudioSlotIndex = playerContext.PlayerSlotController.SlotIndex;
        return true;
      }
    }

    public void Stop()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Stop();
    }

    public void Pause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Pause();
    }

    public void Play()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Play();
    }

    public void TogglePlayPause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.TogglePlayPause();
    }

    public void Restart()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Restart();
    }

    public void SeekForward()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.SeekForward();
    }

    public void SeekBackward()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.SeekBackward();
    }

    public bool PreviousItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return false;
      return playerContext.PreviousItem();
    }

    public bool NextItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return false;
      return playerContext.NextItem();
    }

    public void ToggleCurrentPlayer()
    {
      lock (SyncObj)
      {
        if (_currentPlayerIndex != -1)
          CurrentPlayerIndex = 1 - _currentPlayerIndex;
        CheckCurrentPlayerSlot();
      }
    }

    public void SwitchPipPlayers()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int numActive = playerManager.NumActiveSlots;
        if (numActive > 1 &&
            GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT).AVType == AVType.Video &&
            GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT).AVType == AVType.Video)
          playerManager.SwitchSlots();
      }
    }

    #endregion
  }
}
