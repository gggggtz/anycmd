﻿
namespace Anycmd.Engine.Ac.Messages.Infra
{
    using InOuts;

    public class UpdateResourceCommand : UpdateEntityCommand<IResourceTypeUpdateIo>, IAnycmdCommand
    {
        public UpdateResourceCommand(IResourceTypeUpdateIo input)
            : base(input)
        {

        }
    }
}