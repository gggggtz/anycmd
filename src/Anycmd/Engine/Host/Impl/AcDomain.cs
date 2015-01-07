﻿
namespace Anycmd.Engine.Host.Impl
{
    using Anycmd.Rdb;
    using Bus;
    using Edi;
    using Engine.Ac;
    using Events;
    using Logging;
    using System;

    /// <summary>
    /// 
    /// </summary>
    public abstract class AcDomain : AnycmdServiceContainer, IAcDomain
    {
        private static readonly object Locker = new object();
        private bool _pluginsLoaded;

        private readonly Guid _id = Guid.NewGuid();

        public Guid Id
        {
            get { return _id; }
        }

        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime StartedAt { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime AfterInitAt { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime ReadyAt { get; protected set; }

        protected AcDomain()
        {
            lock (Locker)
            {
                this.Name = "DefaultAcDomain";
                this.StartedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual AcDomain Init()
        {
            this.Config = Conventions;
            OnConfigLoad();

            Configure();

            OnAfterInit();

            return this;
        }

        private HostConvention _conventions;

        /// <summary>
        /// Gets the conventions.
        /// </summary>
        /// <value>The conventions.</value>
        public virtual HostConvention Conventions
        {
            get { return _conventions ?? (_conventions = new HostConvention()); }
            set { _conventions = value; }
        }

        private IAppConfig _config;
        /// <summary>
        /// 
        /// </summary>
        public IAppConfig Config
        {
            get
            {
                return _config;
            }
            private set
            {
                _config = value;
                OnAfterConfigChanged();
            }
        }

        public INodeHost NodeHost { get; protected set; }

        #region 属性
        public IMessageDispatcher MessageDispatcher { get; protected set; }

        public ICommandBus CommandBus { get; protected set; }

        public IEventBus EventBus { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IRdbs Rdbs { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IDbTables DbTables { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IDbViews DbViews { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IDbTableColumns DbTableColumns { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IDbViewColumns DbViewColumns { get; protected set; }

        /// <summary>
        /// 应用系统。
        /// </summary>
        public IAppSystemSet AppSystemSet { get; protected set; }

        /// <summary>
        /// 系统按钮
        /// </summary>
        public IButtonSet ButtonSet { get; protected set; }

        /// <summary>
        /// 系统用户/账户
        /// </summary>
        public ISysUserSet SysUsers { get; protected set; }

        /// <summary>
        /// 系统字典
        /// </summary>
        public IDicSet DicSet { get; protected set; }

        /// <summary>
        /// 系统模型
        /// </summary>
        public IEntityTypeSet EntityTypeSet { get; protected set; }

        /// <summary>
        /// 系统操作
        /// </summary>
        public IFunctionSet FunctionSet { get; protected set; }

        /// <summary>
        /// 系统组织结构
        /// </summary>
        public IOrganizationSet OrganizationSet { get; protected set; }

        /// <summary>
        /// 系统界面视图
        /// </summary>
        public IUiViewSet UiViewSet { get; protected set; }

        /// <summary>
        /// 系统资源
        /// </summary>
        public IResourceTypeSet ResourceTypeSet { get; protected set; }

        /// <summary>
        /// 权限集
        /// </summary>
        public IPrivilegeSet PrivilegeSet { get; protected set; }

        /// <summary>
        /// 菜单集
        /// </summary>
        public IMenuSet MenuSet { get; protected set; }

        /// <summary>
        /// 角色集
        /// </summary>
        public IRoleSet RoleSet { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public ISsdSetSet SsdSetSet { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        public IDsdSetSet DsdSetSet { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public IGroupSet GroupSet { get; protected set; }
        #endregion

        /// <summary>
        /// Config has changed
        /// </summary>
        public virtual void OnAfterConfigChanged()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract void Configure();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public virtual void SetConfig(IAppConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void OnConfigLoad()
        {
        }

        /// <summary>
        /// 当Configure方法调用后。
        /// </summary>
        public void OnAfterInit()
        {
            AfterInitAt = DateTime.UtcNow;

            LoadPlugin(Conventions.Plugins.ToArray());
            _pluginsLoaded = true;

            ReadyAt = DateTime.UtcNow;
        }

        public T DeserializeFromString<T>(string value)
        {
            // TODO:移除对ServiceStack.Text的依赖
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(value);
        }

        public string SerializeToString<T>(T value)
        {
            return ServiceStack.Text.JsonSerializer.SerializeToString<T>(value);
        }

        /// <summary>
        /// this.DirectEventBus.Publish(evnt);
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="evnt"></param>
        public void PublishEvent<TEvent>(TEvent evnt) where TEvent : class, IEvent
        {
            this.EventBus.Publish(evnt);
        }

        /// <summary>
        /// this.DirectEventBus.Commit();
        /// </summary>
        public void CommitEventBus()
        {
            this.EventBus.Commit();
        }

        /// <summary>
        /// this.DirectCommandBus.Publish(command);
        /// this.DirectCommandBus.Commit();
        /// </summary>
        /// <param name="command"></param>
        public void Handle(IAnycmdCommand command)
        {
            this.CommandBus.Publish(command);
            this.CommandBus.Commit();
        }

        private ILoggingService _loggingService;
        public ILoggingService LoggingService
        {
            get { return _loggingService ?? (_loggingService = this.GetRequiredService<ILoggingService>()); }
        }

        #region plugin
        /// <summary>
        /// 根据插件类型获取域内插件地址
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        public virtual string GetPluginBaseDirectory(PluginType pluginType)
        {
            return Conventions.PluginBaseDirectory(pluginType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plugins"></param>
        protected virtual void LoadPlugin(params IPlugin[] plugins)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Register(this);
                }
                catch (Exception ex)
                {
                    LoggingService.Error("Error loading plugin " + plugin.GetType().Name, ex);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plugins"></param>
        protected void AddPlugin(params IPlugin[] plugins)
        {
            if (_pluginsLoaded)
            {
                LoadPlugin(plugins);
            }
            else
            {
                foreach (var plugin in plugins)
                {
                    Conventions.Plugins.Add(plugin);
                }
            }
        }
        #endregion

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            if (!(obj is IAcDomain))
            {
                return false;
            }
            return ((IAcDomain)obj).Id == this.Id;
        }
    }
}