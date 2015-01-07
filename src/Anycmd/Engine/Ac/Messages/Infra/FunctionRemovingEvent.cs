﻿
namespace Anycmd.Engine.Ac.Messages.Infra
{
    using Abstractions.Infra;
    using Events;

    public class FunctionRemovingEvent: DomainEvent
    {
        public FunctionRemovingEvent(FunctionBase source)
            : base(source)
        {
        }
    }
}