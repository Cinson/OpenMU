﻿// <copyright file="ServerInfoRequestHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.ConnectServer.PacketHandler
{
    using System;
    using log4net;

    /// <summary>
    /// Handles the server info request of a client, which means the client wants to know the connect data of the server it just clicked on.
    /// </summary>
    internal class ServerInfoRequestHandler : IPacketHandler<Client>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServerInfoRequestHandler));
        private readonly IConnectServer connectServer;
        private readonly Settings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfoRequestHandler"/> class.
        /// </summary>
        /// <param name="connectServer">The connect server.</param>
        public ServerInfoRequestHandler(IConnectServer connectServer)
        {
            this.connectServer = connectServer;
            this.settings = connectServer.Settings;
        }

        /// <inheritdoc/>
        public void HandlePacket(Client client, Span<byte> packet)
        {
            var serverId = (ushort)(packet[4] | packet[5] << 8);
            Log.DebugFormat("Client {0}:{1} requested Connection Info of ServerId {2}", client.Address, client.Port, serverId);
            if (client.ServerInfoRequestCount >= this.settings.MaxIpRequests)
            {
                Log.Debug($"Client {client.Address}:{client.Port} reached max ip requests.");
                client.Connection.Disconnect();
            }

            if (this.connectServer.ConnectInfos.TryGetValue(serverId, out byte[] connectInfo))
            {
                client.Connection.Send(connectInfo);
            }
            else
            {
                Log.Debug($"Client {client.Address}:{client.Port}: Connection Info not found, sending Server List instead.");
                client.Connection.Send(this.connectServer.ServerList.Serialize());
            }

            client.SendHello();
            client.ServerInfoRequestCount++;
        }
    }
}
