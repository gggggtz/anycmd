﻿
namespace Anycmd.Engine.Ac.Messages.Infra
{
    using Abstractions.Infra;
    using Events;
    using InOuts;
    using System;

    /// <summary>
    /// 
    /// </summary>
    public class AppSystemUpdatedEvent : DomainEvent
    {
        public AppSystemUpdatedEvent(AppSystemBase source, IAppSystemUpdateIo input)
            : base(source)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            this.Input = input;
        }

        public IAppSystemUpdateIo Input { get; private set; }
    }
}