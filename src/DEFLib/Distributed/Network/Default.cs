﻿using Defcore.Distributed.Manager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Defcore.Distributed.Network
{
    /// <summary>
    /// Object passed between Default Receiver
    /// </summary>
    internal class DefaultDataComm : DataReceivedEventArgs
    {
        public MessageType Protocol { get; }
        private static readonly Dictionary<string, MessageType> MessageMap =
            new Dictionary<string, MessageType> {
                { "node", MessageType.Node },
                { "query", MessageType.Query },
                { "job", MessageType.Job },
                { "id", MessageType.Id }
            };

        public enum MessageType
        {
            Unknown,
            Id,
            Node,
            Query,
            Job
        }

        public DefaultDataComm(string msg)
            : base(msg)
        {
            MessageType m;
            MessageMap.TryGetValue(Args[0], out m);
            Protocol = m;
        }
    }

    /// <summary>
    /// A default receiver for a master node before
    /// the connection type is known.
    /// </summary>
    /// <remarks>
    /// When a connection is made to a server there
    /// is no way of knowing immediately if that connection
    /// belongs. Default receivers know enough to hand off
    /// connection to a proper receiver. For the master node
    /// manager there could be a query, job or node connection.
    /// </remarks>
    internal class DefaultReceiver : AbstractReceiver
    {
        public NodeManager Manager;

        public DefaultReceiver(NodeManager manager)
        {
            Manager = manager;
        }

        public override DataReceivedEventArgs CreateDataReceivedEvent(string data)
        {
            return new DefaultDataComm(data);
        }

        public override void HandleAdditionalReceiving(object sender, DataReceivedEventArgs e)
        {
            var defaultDataComm = e as DefaultDataComm;
            if (defaultDataComm != null)
                switch (defaultDataComm.Protocol)
                {
                    case DefaultDataComm.MessageType.Node:
                        // hand off to proper sender and receiver
                        Proxy.HandOffSendReceive(new NodeManagerReceiver(),
                            new NodeManagerSender(Manager), ConnectionType.Node);
                        // leave and stop this thread.
                        Manager.AddConnectedNode(Proxy);
                        DoneReceiving = true;
                        return;
                    case DefaultDataComm.MessageType.Job:
                        // hand off to proper sender and receiver
                        Proxy.HandOffSendReceive(new NodeManagerReceiver(),
                            new NodeManagerSender(Manager), ConnectionType.Job);
                        // tell the job to submit their job info
                        Proxy.QueueDataEvent(new NodeManagerComm("submit"));
                        // leave and stop this thread.
                        DoneReceiving = true;
                        return;
                    case DefaultDataComm.MessageType.Query:
                        // hand off to proper sender and receiver
                        Proxy.HandOffSendReceive(new NodeManagerReceiver(), 
                            new NodeManagerSender(Manager), ConnectionType.Query);
                        // leave and stop this thread.
                        DoneReceiving = true;
                        return;
                }
        }

    }

    /// <summary>
    /// The default sender for the master node manager
    /// before hand off.
    /// </summary>
    internal class DefaultSender : AbstractSender
    {
        // ConcurrentQueue for TryDequeue method
        readonly ConcurrentQueue<DefaultDataComm> _messageQueue = new ConcurrentQueue<DefaultDataComm>();
        private NodeManager _manager;

        /// <summary>
        /// Constructor to add initial message of id
        /// on start up that message will be send out to the connected host
        /// to gather identification info.
        /// </summary>
        public DefaultSender(NodeManager nodeManager)
        {
            _manager = nodeManager;
            //this._manager = manager;
            _messageQueue.Enqueue(new DefaultDataComm("id"));
        }

        /// <summary>
        /// Handles receive event, adds message to queue
        /// which is checked by running thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void HandleReceiverEvent(object sender, DataReceivedEventArgs e)
        {
            _messageQueue.Enqueue(e as DefaultDataComm);
        }

        /// <summary>
        /// Default sender uses thread with run method.
        /// </summary>
        public override void Run()
        {
            while (!DoneSending)
            {
                // grab the message
                DefaultDataComm data;
                if (_messageQueue.TryDequeue(out data))
                {
                    // if we don't know who this is, the first message
                    // is an Id, request id.
                    if (data.Protocol == DefaultDataComm.MessageType.Unknown 
                        || data.Protocol == DefaultDataComm.MessageType.Id)
                    {
                        _manager.Logger.Log("Default Handler: Requesting Id");
                        SendMessage(new [] { "id" });
                    }
                    else
                    {
                        // otherwise we know what kind it is,
                        // and the above receive method has
                        // already called "hand off" with new sender
                        // and receivers.
                        DoneSending = true;
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
  
}
