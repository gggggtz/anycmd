﻿
namespace Anycmd.Engine.Edi.Messages
{
    using Abstractions;
    using Events;

    /// <summary>
    /// 
    /// </summary>
    public sealed class ArchivedEvent : DomainEvent
    {
        public ArchivedEvent(ArchiveBase source)
            : base(source)
        {
        }
    }
}