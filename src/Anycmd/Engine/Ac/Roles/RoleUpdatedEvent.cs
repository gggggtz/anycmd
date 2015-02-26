﻿
namespace Anycmd.Engine.Ac.Roles
{
    using Roles;
    using Events;
    using InOuts;
    using System;

    public class RoleUpdatedEvent : DomainEvent
    {
        public RoleUpdatedEvent(IAcSession acSession, RoleBase source, IRoleUpdateIo output)
            : base(acSession, source)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            this.Output = output;
        }

        public IRoleUpdateIo Output { get; private set; }
    }
}