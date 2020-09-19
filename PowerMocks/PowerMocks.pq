// This file contains your Data Connector logic
section PowerMocks;

[DataSource.Kind="PowerMocks", Publish="PowerMocks.Publish"]
shared PowerMocks.Contents = (optional message as text) =>
    let
        _message = if (message <> null) then message else "(no message)",
        a = "Hello from PowerMocks: " & _message
    in
        a;

// Data Source Kind description
PowerMocks = [
    Authentication = [
        // Key = [],
        // UsernamePassword = [],
        // Windows = [],
        Implicit = []
    ],
    Label = Extension.LoadString("DataSourceLabel")
];

// Data Source UI publishing description
PowerMocks.Publish = [
    Beta = true,
    Category = "Other",
    ButtonText = { Extension.LoadString("ButtonTitle"), Extension.LoadString("ButtonHelp") },
    LearnMoreUrl = "https://powerbi.microsoft.com/",
    SourceImage = PowerMocks.Icons,
    SourceTypeImage = PowerMocks.Icons
];

PowerMocks.Icons = [
    Icon16 = { Extension.Contents("PowerMocks16.png"), Extension.Contents("PowerMocks20.png"), Extension.Contents("PowerMocks24.png"), Extension.Contents("PowerMocks32.png") },
    Icon32 = { Extension.Contents("PowerMocks32.png"), Extension.Contents("PowerMocks40.png"), Extension.Contents("PowerMocks48.png"), Extension.Contents("PowerMocks64.png") }
];
