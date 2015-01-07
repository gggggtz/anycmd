﻿
namespace Anycmd.Engine.Ac.InOuts
{
    using System;

    /// <summary>
    /// 更新动态职责分离角色集时的输入或输出参数类型。
    /// </summary>
    public class DsdSetUpdateIo : IDsdSetUpdateIo
    {
        public string Name { get; set; }

        public int DsdCard { get; set; }

        public int IsEnabled { get; set; }

        public string Description { get; set; }

        public Guid Id { get; set; }
    }
}