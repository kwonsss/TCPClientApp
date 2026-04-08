using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TcpServerApp
{
    public record Header(
        [property: JsonPropertyName("messageType")] string MessageType,
        [property: JsonPropertyName("actionType")] string ActionType,
        [property: JsonPropertyName("timestamp")] string Timestamp,
        [property: JsonPropertyName("sender")] string Sender,
        [property: JsonPropertyName("receiver")] string Receiver
    );

    public record Unit(
        [property: JsonPropertyName("unitId")] string UnitId,
        [property: JsonPropertyName("connectionStatus")] string ConnectionStatus
    );

    public record Module(
        [property: JsonPropertyName("moduleId")] string ModuleId,
        [property: JsonPropertyName("moduleIndex")] string ModuleIndex,
        [property: JsonPropertyName("connectionStatus")] string ConnectionStatus,
        [property: JsonPropertyName("units")] Unit[] Units
    );

    public record Body(
        [property: JsonPropertyName("modules")] Module[] Modules
    );

    public record WorkOrderMessage(
        [property: JsonPropertyName("header")] Header Header,
        [property: JsonPropertyName("body")] Body Body
    );
}
