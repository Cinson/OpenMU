// <copyright file="Proxy.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.Analyzer
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using log4net;

    /// <summary>
    /// A proxy which is a man-in-the-middle between a client and server connection.
    /// It allows to capture the unencrypted network traffic.
    /// </summary>
    public class Proxy : INotifyPropertyChanged
    {
        /// <summary>
        /// The connection to the client.
        /// </summary>
        private readonly IConnection clientConnection;

        /// <summary>
        /// The connection to the server.
        /// </summary>
        private readonly IConnection serverConnection;

        /// <summary>
        /// The invoke action to run an action on the UI thread.
        /// </summary>
        private readonly Action<Delegate> invokeAction;

        /// <summary>
        /// The logger for this proxied connection.
        /// </summary>
        private readonly ILog log;

        private readonly string clientName;

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Proxy"/> class.
        /// </summary>
        /// <param name="clientConnection">The client connection.</param>
        /// <param name="serverConnection">The server connection.</param>
        /// <param name="invokeAction">The invoke action to run an action on the UI thread.</param>
        public Proxy(IConnection clientConnection, IConnection serverConnection, Action<Delegate> invokeAction)
        {
            this.clientConnection = clientConnection;
            this.serverConnection = serverConnection;
            this.invokeAction = invokeAction;
            this.log = LogManager.GetLogger("Proxy_" + this.clientConnection);
            this.clientName = this.clientConnection.ToString();
            this.Name = this.clientName;
            this.clientConnection.PacketReceived += this.ClientPacketReceived;
            this.serverConnection.PacketReceived += this.ServerPacketReceived;
            this.clientConnection.Disconnected += this.ClientDisconnected;
            this.serverConnection.Disconnected += this.ServerDisconnected;
            this.log.Info("Proxy initialized.");
            this.clientConnection.BeginReceive();
            this.serverConnection.BeginReceive();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the packet list of all captured packets.
        /// </summary>
        public BindingList<Packet> PacketList { get; } = new BindingList<Packet>();

        /// <summary>
        /// Gets or sets the name of the proxied connection.
        /// </summary>
        public string Name
        {
            get => this.name;

            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Disconnects the connection to the server and therefore indirectly also to the client.
        /// </summary>
        public void Disconnect()
        {
            this.serverConnection.Disconnect();
        }

        /// <summary>
        /// Called when a property value changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.invokeAction((Action)(() => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))));
        }

        private void ServerDisconnected(object sender, EventArgs e)
        {
            this.log.Info("The server connection closed.");
            this.clientConnection.Disconnect();
        }

        private void ClientDisconnected(object sender, EventArgs e)
        {
            this.log.Info("The client connected closed");
            this.serverConnection.Disconnect();
            this.Name = this.clientName + " [Disconnected]";
        }

        private void ServerPacketReceived(object sender, Span<byte> data)
        {
            this.clientConnection.Send(data.ToArray()); // TODO: remove ToArray?
            var packet = new Packet(data.ToArray(), false);
            this.log.Info(packet.ToString());
            this.invokeAction((Action)(() => this.PacketList.Add(packet)));
        }

        private void ClientPacketReceived(object sender, Span<byte> data)
        {
            var packet = new Packet(data.ToArray(), true);
            this.log.Info(packet.ToString());
            this.serverConnection.Send(data.ToArray()); // TODO: remove ToArray?
            this.invokeAction((Action)(() => this.PacketList.Add(packet)));
        }
    }
}