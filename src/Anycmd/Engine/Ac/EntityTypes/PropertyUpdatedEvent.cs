﻿
namespace Anycmd.Engine.Ac.EntityTypes
{
    using Abstractions.Infra;
    using Events;
    using InOuts;

    /// <summary>
    /// 
    /// </summary>
    public class PropertyUpdatedEvent : DomainEvent
    {
        public PropertyUpdatedEvent(IAcSession acSession, PropertyBase source, IPropertyUpdateIo input)
            : base(acSession, source)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input");
            }
            this.Input = input;
        }

        public IPropertyUpdateIo Input { get; private set; }
    }
}