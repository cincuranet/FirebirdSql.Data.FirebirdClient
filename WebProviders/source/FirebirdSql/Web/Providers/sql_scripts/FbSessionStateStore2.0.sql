SET SQL DIALECT 3;

CREATE TABLE SESSIONS (
    SESSIONID           VARCHAR(80) CHARACTER SET OCTETS NOT NULL,
    APPLICATIONNAME     VARCHAR(100) CHARACTER SET UTF8 NOT NULL,
    CREATED             TIMESTAMP,
    EXPIRES             TIMESTAMP,
    LOCKDATE            TIMESTAMP,
    LOCKID              INTEGER,
    TIMEOUT             INTEGER,
    LOCKED              SMALLINT,
    SESSIONITEMS        BLOB SUB_TYPE TEXT SEGMENT SIZE 4096,
    FLAGS               INTEGER
);

CREATE UNIQUE INDEX SESSIONS_IDX1 ON SESSIONS (SESSIONID, APPLICATIONNAME); 