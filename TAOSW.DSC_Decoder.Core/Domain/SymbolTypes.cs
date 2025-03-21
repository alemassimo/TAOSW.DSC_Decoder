namespace TAOSW.DSC_Decoder.Core.Domain
{
    public enum FormatSpecifier
    {
        DistressAlert = 112, // symbol No. 112 for a “distress” alert
        AllShipsCall = 116, // symbol No. 116 for an “all ships” call
        GroupCall = 114, // symbol No. 114 for a selective call to a group of ships having a common interest
        IndividualStationCall = 120, // symbol No. 120 for a selective call to a particular individual station
        GeographicAreaGroupCall = 102, // symbol No. 102 for a selective call to a group of ships in a particular geographic area
        AutomaticServiceCall = 123,
        Error = -1 // symbol No. 123 for a selective call to a particular individual station using the automatic service
    }

    public enum CategoryOfCall
    {
        Routine = 100,
        Safety = 108,
        Urgency = 110,
        Distress = 112,
        Error = -1
    }

    public enum NatureOfDistress
    {
        FireExplosion = 100, // Fire, explosion
        Flooding = 101, // Flooding
        Collision = 102, // Collision
        Grounding = 103, // Grounding
        ListingInDangerOfCapsizing = 104, // Listing, in danger of capsizing
        Sinking = 105, // Sinking
        DisabledAndAdrift = 106, // Disabled and adrift
        UndesignatedDistress = 107, // Undesignated distress
        AbandoningShip = 108, // Abandoning ship
        PiracyArmedRobberyAttack = 109, // Piracy/armed robbery attack
        ManOverboard = 110, // Man overboard
        Error = -1
    }

    public enum FirstCommand
    {
        AllModesTP = 100, // F3E/G3E All modes TP
        DuplexTP = 101, // F3E/G3E duplex TP
        Polling = 103, // Polling
        UnableToComply = 104, // Unable to comply
        EndOfCall = 105, // End of call
        Data = 106, // Data
        J3ETP = 109, // J3E TP
        DistressAcknowledgement = 110, // Distress acknowledgement
        DistressAlertRelay = 112, // Distress alert relay
        TTYFEC = 113, // F1B/J2B TTY-FEC
        TTYARQ = 115, // F1B/J2B TTY-ARQ
        Test = 118, // Test
        ShipPositionOrLocationRegistrationUpdating = 121, // Ship position or location registration updating
        NoInformation = 126, // No informatio
        Error = -1
    }

    //symbol No. 117 if the call requires acknowledgement (Acknowledge RQ), used for 
    //individual and automatic calls only;
    //symbol No. 122 if the sequence is an answer to a call that requires acknowledgement
    //(Acknowledge BQ), used for individual and automatic calls and all distress alert relay
    //acknowledgements;
    //symbol No. 127 for all other calls.
    public enum EndOfSequence
    {
        AcknowledgeRQ = 117, // Acknowledge RQ
        AcknowledgeBQ = 122, // Acknowledge BQ
        OtherCalls = 127, // Other calls
        Error = -1
    }


    public enum SecondCommand
    {
        NoReasonGiven = 100, // No reason given
        CongestionAtMaritimeSwitchingCentre = 101, // Congestion at maritime switching centre
        Busy = 102, // Busy
        QueueIndication = 103, // Queue indication
        StationBarred = 104, // Station barred
        NoOperatorAvailable = 105, // No operator available
        OperatorTemporarilyUnavailable = 106, // Operator temporarily unavailable
        EquipmentDisabled = 107, // Equipment disabled
        UnableToUseProposedChannel = 108, // Unable to use proposed channel
        UnableToUseProposedMode = 109, // Unable to use proposed mode
        ShipsAndAircraftOfStatesNotPartiesToAnArmedConflict = 110, // Ships and aircraft of States not parties to an armed conflict
        MedicalTransports = 111, // Medical transports
        PayPhonePublicCallOffice = 112, // Pay-phone/public call office
        FacsimileData = 113, // Facsimile/data
        NoRemainingACSSequentialTransmission = 120, // No remaining ACS sequential transmission
        OneTimeRemainingACSSequentialTransmission = 121, // 1 time remaining ACS sequential transmission
        TwoTimesRemainingACSSequentialTransmission = 122, // 2 times remaining ACS sequential transmission
        ThreeTimesRemainingACSSequentialTransmission = 123, // 3 times remaining ACS sequential transmission
        FourTimesRemainingACSSequentialTransmission = 124, // 4 times remaining ACS sequential transmission
        FiveTimesRemainingACSSequentialTransmission = 125, // 5 times remaining ACS sequential transmission
        NoInformation = 126, // No information
        Error = -1
    }
}
