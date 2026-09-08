CREATE DATABASE RACEDAYDB;
USE RACEDAYDB;

CREATE TABLE Roles(RoleId INT IDENTITY(1,1) PRIMARY KEY,
RoleName VARCHAR(20) NOT NULL UNIQUE);

CREATE TABLE Users(UserId INT IDENTITY(1,1) PRIMARY KEY,
FullName VARCHAR(100) NOT NULL,
Email VARCHAR(100) NOT NULL UNIQUE,
PasswordHash VARCHAR(255) NOT NULL,
RoleId INT NOT NULL,
Createdat DATETIME NOT NULL DEFAULT GETDATE(),
FOREIGN KEY(RoleId) REFERENCES Roles(RoleId));

CREATE TABLE Events (EventId INT IDENTITY(1,1) PRIMARY KEY,
EventName VARCHAR(150) NOT NULL,
EventDate DATE NOT NULL,
Location VARCHAR(150) NOT NULL,
Description VARCHAR(MAX) NULL,
OrganiserId INT NOT NULL,
CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
FOREIGN KEY(OrganiserId) REFERENCES USERS(UserId));

CREATE TABLE Categories(CategoryId INT IDENTITY(1,1) PRIMARY KEY,
EventId INT NOT NULL,
CategoryName VARCHAR(50) NOT NULL,
DistanceKm DECIMAL(5,2) NOT NULL,
MaxParticipants INT NOT NULL DEFAULT 100,
EntryFee DECIMAL(8,2) NOT NULL DEFAULT 0,
FOREIGN KEY(EventId) REFERENCES Events(EventId));

CREATE TABLE Enrolments(EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
ParticipantId INT NOT NULL,
CategoryId INT NOT NULL,
EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
Status VARCHAR(20) NOT NULL DEFAULT'Confirmed'
FOREIGN KEY(ParticipantId) REFERENCES Users(UserId),
FOREIGN KEY(CategoryId) REFERENCES Categories(CategoryId));

CREATE TABLE Results(ResultId INT IDENTITY(1,1) PRIMARY KEY,
EnrolmentId INT NOT NULL UNIQUE,
FinshTime TIME NULL,
Position INT NULL,
CapturedByUserId INT NOT NULL,
CapturedAt DATETIME NOT NULL DEFAULT GETDATE(),
FOREIGN KEY(EnrolmentId) REFERENCES Enrolments(EnrolmentId),
FOREIGN KEY(CapturedByUserId) REFERENCES Users(UserId));

--Tests--
INSERT INTO Roles(RoleName) VALUES('Organiser'),('Participant');

INSERT INTO Users(FullName,Email,PasswordHash,RoleId)VALUES
('Bohlale Lukhele','lukhele.bo@raceday.co.za','HASH_PLACEHOLDER_1',1),--Organiser--
('Mlando Lukhele','lukhele.mlando@raceday.co.za','HASH_PLACEHOLDER_2',1),--Organiser--
('Nompumelelo Zwane','zwanenoms@gmail.com','HASH_PLACEHOLDER_3',2),--Participant--
('Bohlale Lukhele','lukhele.bo@raceday.co.za','HASH_PLACEHOLDER_4',2);--Participant--

INSERT INTO Events(EventName,EventDate,Location,Description,OrganiserId)VALUES
('Johannesburg City Run','2026-10-18','Johannesburg, Gauteng','Annual Road Running Event in JHB CBD',1),
('Durban Beachfront Cycle Tour','2026-11-08','Durban,KwaZulu-Natal','Scenic cycling tour in Durban beachfront.',1),
('Cape Town Marathon','2026-12-15','Cape Town,Western Cape','The Cape Town features a gruelling 42KM marathon.',2);

INSERT INTO Categories (EventId, CategoryName, DistanceKm, MaxParticipants, EntryFee) VALUES
(1, '5km Fun Run', 5.00, 500, 100.00),
(1, '10km Road Race', 10.00, 300, 150.00),
(2, '21km Half Cycle', 21.00, 200, 250.00),
(2, '42km Full Cycle', 42.00, 150, 350.00),
(3, '10km Charity Walk', 10.00, 400, 80.00);

