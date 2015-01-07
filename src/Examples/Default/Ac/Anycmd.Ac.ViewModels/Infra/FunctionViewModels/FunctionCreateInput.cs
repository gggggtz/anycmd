﻿
namespace Anycmd.Ac.ViewModels.Infra.FunctionViewModels
{
    using Engine;
    using Engine.Ac.InOuts;
    using System;
    using System.ComponentModel.DataAnnotations;

    public class FunctionCreateInput : EntityCreateInput, IFunctionCreateIo
    {
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public Guid ResourceTypeId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public string Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public bool IsManaged { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public int IsEnabled { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public string Description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public Guid DeveloperId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Required]
        public int SortCode { get; set; }
    }
}