﻿
namespace Anycmd.Engine.Ac
{
    using Abstractions.Infra;
    using Host;
    using System;
    using Util;

    /// <summary>
    /// 界面视图业务实体类型。
    /// </summary>
    public sealed class UiViewState : StateObject<UiViewState>, IUiView, IStateObject
    {
        public static readonly UiViewState Empty = new UiViewState(Guid.Empty)
        {
            _acDomain = EmptyAcDomain.SingleInstance,
            _createOn = SystemTime.MinDate,
            _icon = string.Empty,
            _tooltip = string.Empty
        };

        private IAcDomain _acDomain;
        private string _tooltip;
        private string _icon;
        private DateTime? _createOn;

        private UiViewState(Guid id) : base(id) { }

        public static UiViewState Create(IAcDomain host, UiViewBase view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            return new UiViewState(view.Id)
            {
                _acDomain = host,
                _tooltip = view.Tooltip,
                _createOn = view.CreateOn,
                _icon = view.Icon,
            };
        }

        public IAcDomain AcDomain
        {
            get { return _acDomain; }
        }

        public string Tooltip
        {
            get { return _tooltip; }
        }

        public string Icon
        {
            get { return _icon; }
        }

        public DateTime? CreateOn
        {
            get { return _createOn; }
        }

        protected override bool DoEquals(UiViewState other)
        {
            return Id == other.Id &&
                Tooltip == other.Tooltip &&
                Icon == other.Icon;
        }
    }
}