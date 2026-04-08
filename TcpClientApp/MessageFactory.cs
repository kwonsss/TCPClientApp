using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;

namespace TcpServerApp
{
    public static class MessageFactory
    {
        public static WorkOrderMessage CreateWorkOrderDispatch()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            return new WorkOrderMessage(
                Header: new Header(
                    MessageType: "Request",
                    ActionType: "WorkOrderDispatch",
                    Timestamp: timestamp,
                    Sender: "T IOS",
                    Receiver: "L IOS"
                ),
                Body: new Body(
                    Modules:
                    [
                        new Module(
                        ModuleId:         "6-1 Module",
                        ModuleIndex:      "1",
                        ConnectionStatus: "Connected",
                        Units:
                        [
                            new Unit("6-1 제어",  "Connected"),
                            new Unit("6-1 M9600", "Connected"),
                        ]
                    )
                    ]
                )
            );
        }
    }
}
