﻿
namespace Anycmd.Engine.Ac.Messages.Infra
{
    using Abstractions.Infra;
    using Events;
    using InOuts;

    /// <summary>
    /// 
    /// </summary>
    public class FunctionUpdatedEvent : DomainEvent
    {
        public FunctionUpdatedEvent(FunctionBase source, IFunctionUpdateIo input)
            : base(source)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input");
            }
            this.Input = input;
        }

        public IFunctionUpdateIo Input { get; private set; }
    }
}