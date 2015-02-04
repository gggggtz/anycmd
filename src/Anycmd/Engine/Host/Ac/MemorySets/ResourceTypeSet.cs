﻿
namespace Anycmd.Engine.Host.Ac.MemorySets
{
    using Bus;
    using Engine.Ac;
    using Engine.Ac.Abstractions;
    using Engine.Ac.Abstractions.Infra;
    using Engine.Ac.InOuts;
    using Engine.Ac.Messages.Infra;
    using Exceptions;
    using Host;
    using Infra;
    using Repositories;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Util;

    /// <summary>
    /// 资源上下文
    /// </summary>
    internal sealed class ResourceTypeSet : IResourceTypeSet, IMemorySet
    {
        public static readonly IResourceTypeSet Empty = new ResourceTypeSet(EmptyAcDomain.SingleInstance);

        private readonly Dictionary<AppSystemState, Dictionary<string, ResourceTypeState>> _dicByCode = new Dictionary<AppSystemState,Dictionary<string,ResourceTypeState>>();
        private readonly Dictionary<Guid, ResourceTypeState> _dicById = new Dictionary<Guid, ResourceTypeState>();
        private bool _initialized = false;

        private readonly Guid _id = Guid.NewGuid();
        private readonly IAcDomain _acDomain;

        public Guid Id
        {
            get { return _id; }
        }

        internal ResourceTypeSet(IAcDomain acDomain)
        {
            if (acDomain == null)
            {
                throw new ArgumentNullException("acDomain");
            }
            if (acDomain.Equals(EmptyAcDomain.SingleInstance))
            {
                _initialized = true;
            }
            this._acDomain = acDomain;
            new MessageHandler(this).Register();
        }

        public bool TryGetResource(AppSystemState appSystem, string resourceTypeCode, out ResourceTypeState resource)
        {
            if (!_initialized)
            {
                Init();
            }
            if (appSystem == null)
            {
                throw new ArgumentNullException("appSystem");
            }
            if (string.IsNullOrEmpty(resourceTypeCode))
            {
                throw new ArgumentNullException("resourceTypeCode");
            }
            if (!_dicByCode.ContainsKey(appSystem))
            {
                resource = ResourceTypeState.Empty;
                return false;
            }

            return _dicByCode[appSystem].TryGetValue(resourceTypeCode, out resource);
        }

        public bool TryGetResource(Guid resourceTypeId, out ResourceTypeState resource)
        {
            if (!_initialized)
            {
                Init();
            }
            Debug.Assert(resourceTypeId != Guid.Empty);

            return _dicById.TryGetValue(resourceTypeId, out resource);
        }

        internal void Refresh()
        {
            if (_initialized)
            {
                _initialized = false;
            }
        }

        public IEnumerator<ResourceTypeState> GetEnumerator()
        {
            if (!_initialized)
            {
                Init();
            }
            return _dicById.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (!_initialized)
            {
                Init();
            }
            return _dicById.Values.GetEnumerator();
        }

        private void Init()
        {
            if (_initialized) return;
            lock (this)
            {
                if (_initialized) return;
                _acDomain.MessageDispatcher.DispatchMessage(new MemorySetInitingEvent(this));
                _dicByCode.Clear();
                _dicById.Clear();
                var allResources = _acDomain.RetrieveRequiredService<IOriginalHostStateReader>().GetAllResources();
                foreach (var resource in allResources)
                {
                    AppSystemState appSystem;
                    if (!_acDomain.AppSystemSet.TryGetAppSystem(resource.AppSystemId, out appSystem))
                    {
                        throw new AnycmdException("意外的资源类型应用系统标识" + resource.AppSystemId);
                    }
                    if (!_dicByCode.ContainsKey(appSystem))
                    {
                        _dicByCode.Add(appSystem, new Dictionary<string, ResourceTypeState>(StringComparer.OrdinalIgnoreCase));
                    }
                    if (_dicByCode[appSystem].ContainsKey(resource.Code))
                    {
                        throw new AnycmdException("意外重复的资源标识" + resource.Id);
                    }
                    if (_dicById.ContainsKey(resource.Id))
                    {
                        throw new AnycmdException("意外重复的资源标识" + resource.Id);
                    }
                    var resourceState = ResourceTypeState.Create(resource);
                    _dicByCode[appSystem].Add(resource.Code, resourceState);
                    _dicById.Add(resource.Id, resourceState);
                }
                _initialized = true;
                _acDomain.MessageDispatcher.DispatchMessage(new MemorySetInitializedEvent(this));
            }
        }

        #region MessageHandler
        private class MessageHandler:
            IHandler<AddResourceCommand>,
            IHandler<ResourceTypeAddedEvent>,
            IHandler<UpdateResourceCommand>,
            IHandler<ResourceTypeUpdatedEvent>,
            IHandler<RemoveResourceTypeCommand>,
            IHandler<ResourceTypeRemovedEvent>
        {
            private readonly ResourceTypeSet _set;

            internal MessageHandler(ResourceTypeSet set)
            {
                this._set = set;
            }

            public void Register()
            {
                var messageDispatcher = _set._acDomain.MessageDispatcher;
                if (messageDispatcher == null)
                {
                    throw new ArgumentNullException("AcDomain对象'{0}'尚未设置MessageDispatcher。".Fmt(_set._acDomain.Name));
                }
                messageDispatcher.Register((IHandler<AddResourceCommand>)this);
                messageDispatcher.Register((IHandler<ResourceTypeAddedEvent>)this);
                messageDispatcher.Register((IHandler<UpdateResourceCommand>)this);
                messageDispatcher.Register((IHandler<ResourceTypeUpdatedEvent>)this);
                messageDispatcher.Register((IHandler<RemoveResourceTypeCommand>)this);
                messageDispatcher.Register((IHandler<ResourceTypeRemovedEvent>)this);
            }

            public void Handle(AddResourceCommand message)
            {
                this.Handle(message.AcSession, message.Input, true);
            }

            public void Handle(ResourceTypeAddedEvent message)
            {
                if (message.GetType() == typeof(PrivateResourceAddedEvent))
                {
                    return;
                }
                this.Handle(message.AcSession, message.Input, false);
            }

            private void Handle(IAcSession acSession, IResourceTypeCreateIo input, bool isCommand)
            {
                var acDomain = _set._acDomain;
                var dicByCode = _set._dicByCode;
                var dicById = _set._dicById;
                var resourceRepository = acDomain.RetrieveRequiredService<IRepository<ResourceType>>();
                if (string.IsNullOrEmpty(input.Code))
                {
                    throw new ValidationException("编码不能为空");
                }
                if (!input.Id.HasValue)
                {
                    throw new ValidationException("标识是必须的");
                }
                ResourceType entity;
                lock (this)
                {
                    ResourceTypeState resource;
                    if (acDomain.ResourceTypeSet.TryGetResource(input.Id.Value, out resource))
                    {
                        throw new ValidationException("相同标识的资源已经存在" + input.Id.Value);
                    }
                    AppSystemState appSystem;
                    if (!acDomain.AppSystemSet.TryGetAppSystem(input.AppSystemId, out appSystem))
                    {
                        throw new ValidationException("意外的应用系统标识" + input.AppSystemId);
                    }
                    if (acDomain.ResourceTypeSet.TryGetResource(appSystem, input.Code, out resource))
                    {
                        throw new ValidationException("重复的资源编码" + input.Code);
                    }

                    entity = ResourceType.Create(input);

                    var state = ResourceTypeState.Create(entity);
                    if (!dicByCode.ContainsKey(appSystem))
                    {
                        dicByCode.Add(appSystem, new Dictionary<string, ResourceTypeState>(StringComparer.OrdinalIgnoreCase));
                    }
                    if (!dicByCode[appSystem].ContainsKey(entity.Code))
                    {
                        dicByCode[appSystem].Add(entity.Code, state);
                    }
                    if (!dicById.ContainsKey(entity.Id))
                    {
                        dicById.Add(entity.Id, state);
                    }
                    if (isCommand)
                    {
                        try
                        {
                            resourceRepository.Add(entity);
                            resourceRepository.Context.Commit();
                        }
                        catch
                        {
                            if (dicByCode[appSystem].ContainsKey(entity.Code))
                            {
                                dicByCode[appSystem].Remove(entity.Code);
                            }
                            if (dicById.ContainsKey(entity.Id))
                            {
                                dicById.Remove(entity.Id);
                            }
                            resourceRepository.Context.Rollback();
                            throw;
                        }
                    }
                }
                if (isCommand)
                {
                    acDomain.MessageDispatcher.DispatchMessage(new PrivateResourceAddedEvent(acSession, entity, input));
                }
            }

            private class PrivateResourceAddedEvent : ResourceTypeAddedEvent, IPrivateEvent
            {
                internal PrivateResourceAddedEvent(IAcSession acSession, ResourceTypeBase source, IResourceTypeCreateIo input)
                    : base(acSession, source, input)
                {

                }
            }

            public void Handle(UpdateResourceCommand message)
            {
                this.Handle(message.AcSession, message.Input, true);
            }

            public void Handle(ResourceTypeUpdatedEvent message)
            {
                if (message.GetType() == typeof(PrivateResourceUpdatedEvent))
                {
                    return;
                }
                this.Handle(message.AcSession, message.Input, false);
            }

            private void Handle(IAcSession acSession, IResourceTypeUpdateIo input, bool isCommand)
            {
                var acDomain = _set._acDomain;
                var resourceRepository = acDomain.RetrieveRequiredService<IRepository<ResourceType>>();
                if (string.IsNullOrEmpty(input.Code))
                {
                    throw new ValidationException("编码不能为空");
                }
                ResourceTypeState bkState;
                if (!acDomain.ResourceTypeSet.TryGetResource(input.Id, out bkState))
                {
                    throw new NotExistException();
                }
                AppSystemState appSystem;
                if (!acDomain.AppSystemSet.TryGetAppSystem(bkState.AppSystemId, out appSystem))
                {
                    throw new ValidationException("意外的应用系统标识" + bkState.AppSystemId);
                }
                ResourceType entity;
                var stateChanged = false;
                lock (this)
                {
                    ResourceTypeState oldState;
                    if (!acDomain.ResourceTypeSet.TryGetResource(input.Id, out oldState))
                    {
                        throw new NotExistException();
                    }
                    ResourceTypeState resource;
                    if (acDomain.ResourceTypeSet.TryGetResource(appSystem, input.Code, out resource) && resource.Id != input.Id)
                    {
                        throw new ValidationException("重复的资源编码" + input.Code);
                    }
                    entity = resourceRepository.GetByKey(input.Id);
                    if (entity == null)
                    {
                        throw new NotExistException();
                    }

                    entity.Update(input);

                    var newState = ResourceTypeState.Create(entity);
                    stateChanged = newState != bkState;
                    if (stateChanged)
                    {
                        Update(newState);
                    }
                    if (isCommand)
                    {
                        try
                        {
                            resourceRepository.Update(entity);
                            resourceRepository.Context.Commit();
                        }
                        catch
                        {
                            if (stateChanged)
                            {
                                Update(bkState);
                            }
                            resourceRepository.Context.Rollback();
                            throw;
                        }
                    }
                }
                if (isCommand && stateChanged)
                {
                    acDomain.MessageDispatcher.DispatchMessage(new PrivateResourceUpdatedEvent(acSession, entity, input));
                }
            }

            private void Update(ResourceTypeState state)
            {
                var acDomain = _set._acDomain;
                var dicByCode = _set._dicByCode;
                var dicById = _set._dicById;
                AppSystemState appSystem;
                if (!acDomain.AppSystemSet.TryGetAppSystem(state.AppSystemId, out appSystem))
                {
                    throw new ValidationException("意外的应用系统标识" + state.AppSystemId);
                }
                if (!dicByCode.ContainsKey(appSystem))
                {
                    dicByCode.Add(appSystem, new Dictionary<string, ResourceTypeState>(StringComparer.OrdinalIgnoreCase));
                }
                var oldResource = dicById[state.Id];
                var oldKey = oldResource.Code;
                var newKey = state.Code;
                dicById[state.Id] = state;
                if (!dicByCode[appSystem].ContainsKey(newKey))
                {
                    dicByCode[appSystem].Remove(oldKey);
                    dicByCode[appSystem].Add(newKey, state);
                }
                else
                {
                    dicByCode[appSystem][newKey] = state;
                }
            }

            private class PrivateResourceUpdatedEvent : ResourceTypeUpdatedEvent, IPrivateEvent
            {
                internal PrivateResourceUpdatedEvent(IAcSession acSession, ResourceTypeBase source, IResourceTypeUpdateIo input)
                    : base(acSession, source, input)
                {

                }
            }

            public void Handle(RemoveResourceTypeCommand message)
            {
                this.Handle(message.AcSession, message.EntityId, true);
            }

            public void Handle(ResourceTypeRemovedEvent message)
            {
                if (message.GetType() == typeof(PrivateResourceRemovedEvent))
                {
                    return;
                }
                this.Handle(message.AcSession, message.Source.Id, false);
            }

            private void Handle(IAcSession acSession, Guid resourceTypeId, bool isCommand)
            {
                var acDomain = _set._acDomain;
                var dicByCode = _set._dicByCode;
                var dicById = _set._dicById;
                var resourceRepository = acDomain.RetrieveRequiredService<IRepository<ResourceType>>();
                ResourceTypeState bkState;
                if (!acDomain.ResourceTypeSet.TryGetResource(resourceTypeId, out bkState))
                {
                    return;
                }
                ResourceType entity;
                lock (this)
                {
                    ResourceTypeState state;
                    if (!acDomain.ResourceTypeSet.TryGetResource(resourceTypeId, out state))
                    {
                        return;
                    }
                    if (acDomain.FunctionSet.Any(a => a.ResourceTypeId == resourceTypeId))
                    {
                        throw new ValidationException("资源下定义有功能时不能删除");
                    }
                    entity = resourceRepository.GetByKey(resourceTypeId);
                    if (entity == null)
                    {
                        return;
                    }
                    if (dicById.ContainsKey(bkState.Id))
                    {
                        if (isCommand)
                        {
                            acDomain.MessageDispatcher.DispatchMessage(new ResourceTypeRemovingEvent(acSession, entity));
                        }
                        dicById.Remove(bkState.Id);
                    }
                    AppSystemState appSystem;
                    if (!acDomain.AppSystemSet.TryGetAppSystem(state.AppSystemId, out appSystem))
                    {
                        throw new ValidationException("意外的应用系统标识" + state.AppSystemId);
                    }
                    if (dicByCode[appSystem].ContainsKey(bkState.Code))
                    {
                        dicByCode[appSystem].Remove(bkState.Code);
                    }
                    if (isCommand)
                    {
                        try
                        {
                            resourceRepository.Remove(entity);
                            resourceRepository.Context.Commit();
                        }
                        catch
                        {
                            if (!dicById.ContainsKey(bkState.Id))
                            {
                                dicById.Add(bkState.Id, bkState);
                            }
                            if (!dicByCode[appSystem].ContainsKey(bkState.Code))
                            {
                                dicByCode[appSystem].Add(bkState.Code, bkState);
                            }
                            resourceRepository.Context.Rollback();
                            throw;
                        }
                    }
                }
                if (isCommand)
                {
                    acDomain.MessageDispatcher.DispatchMessage(new PrivateResourceRemovedEvent(acSession, entity));
                }
            }

            private class PrivateResourceRemovedEvent : ResourceTypeRemovedEvent, IPrivateEvent
            {
                internal PrivateResourceRemovedEvent(IAcSession acSession, ResourceTypeBase source)
                    : base(acSession, source)
                {

                }
            }
        }
        #endregion
    }
}