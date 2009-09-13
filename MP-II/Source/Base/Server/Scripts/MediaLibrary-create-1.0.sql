-- This script creates the MediaLibrary schema. DO NOT MODIFY!
-- Albert, 2009-08-15

CREATE TABLE SHARES (
  SHARE_ID CHAR(36) NOT NULL,
  NAME VARCHAR(2000) NOT NULL,
  SYSTEM_NAME VARCHAR(100) NOT NULL,
  MEDIAPROVIDER_ID CHAR(36) NOT NULL,
  MEDIAPROVIDER_PATH VARCHAR(2000) NOT NULL,
  IS_ONLINE CHAR(1),

  CONSTRAINT SHARES_PK PRIMARY KEY (SHARE_ID)
);
  
CREATE TABLE SHARES_CATEGORIES (
  SHARE_ID CHAR(36),
  CATEGORYNAME VARCHAR(100),

  CONSTRAINT SHARESCATEGORIES_PK PRIMARY KEY (SHARE_ID, CATEGORYNAME)
  CONSTRAINT SHARESCATEGORIES_SHARE_FK FOREIGN KEY (SHARE_ID) REFERENCES SHARES (SHARE_ID) ON DELETE CASCADE
);

CREATE TABLE SHARES_METADATAEXTRACTORS (
  SHARE_ID CHAR(36),
  METADATAEXTRACTOR_ID CHAR(36),

  CONSTRAINT SHARESMETADATAEXTRACTORS_PK PRIMARY KEY (SHARE_ID, METADATAEXTRACTOR_ID)
  CONSTRAINT SHARESMETADATAEXTRACTORS_SHARE_FK FOREIGN KEY (SHARE_ID) REFERENCES SHARES (SHARE_ID) ON DELETE CASCADE
);

CREATE TABLE MIAM_METADATA (
  MIAMM_ID CHAR(36),
  NAME VARCHAR(2000),
  MIAM_SERIALIZATION VARCHAR(16384),

  CONSTRAINT MIAMM_METADATA_PK PRIMARY KEY (MIAMM_ID)
);

CREATE TABLE MEDIA_ITEMS (
  MEDIA_ITEM_ID BIGINT,
  ...
);
