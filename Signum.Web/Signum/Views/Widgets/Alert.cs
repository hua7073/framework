﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.235
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using Signum.Utilities;
    using Signum.Entities;
    using Signum.Web;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Configuration;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Caching;
    using System.Web.DynamicData;
    using System.Web.SessionState;
    using System.Web.Profile;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;
    using Signum.Entities.Basics;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/Widgets/Alert.cshtml")]
    public class _Page_Signum_Views_Widgets_Alert_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {


        public _Page_Signum_Views_Widgets_Alert_cshtml()
        {
        }
        protected System.Web.HttpApplication ApplicationInstance
        {
            get
            {
                return ((System.Web.HttpApplication)(Context.ApplicationInstance));
            }
        }
        public override void Execute()
        {


 using (var tc = Html.TypeContext<AlertDN>())
{
    
Write(Html.EntityLine(tc, n => n.Entity, el => el.ReadOnly = true));

                                                                 
    
Write(Html.ValueLine(tc, n => n.AlertDate));

                                         
    
Write(Html.ValueLine(tc, n => n.CheckDate));

                                         
    
Write(Html.ValueLine(tc, n => n.Text, vl =>
{
    vl.ValueLineType = ValueLineType.TextArea;
}));

  ;
}
WriteLiteral(" ");


        }
    }
}