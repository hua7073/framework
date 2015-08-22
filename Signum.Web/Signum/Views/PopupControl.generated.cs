﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
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
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 2 "..\..\Signum\Views\PopupControl.cshtml"
    using Signum.Engine.Operations;
    
    #line default
    #line hidden
    using Signum.Entities;
    
    #line 1 "..\..\Signum\Views\PopupControl.cshtml"
    using Signum.Entities.Reflection;
    
    #line default
    #line hidden
    using Signum.Utilities;
    using Signum.Web;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/PopupControl.cshtml")]
    public partial class _Signum_Views_PopupControl_cshtml : System.Web.Mvc.WebViewPage<TypeContext>
    {
        public _Signum_Views_PopupControl_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 4 "..\..\Signum\Views\PopupControl.cshtml"
   
    var modifiable = (ModifiableEntity)Model.UntypedValue; 
    var viewMode = (ViewMode)ViewData[ViewDataKeys.ViewMode];

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteAttribute("id", Tuple.Create(" id=\"", 224), Tuple.Create("\"", 257)
            
            #line 8 "..\..\Signum\Views\PopupControl.cshtml"
, Tuple.Create(Tuple.Create("", 229), Tuple.Create<System.Object, System.Int32>(Model.Compose("panelPopup")
            
            #line default
            #line hidden
, 229), false)
);

WriteLiteral(" class=\"sf-popup-control modal fade\"");

WriteLiteral(" tabindex=\"-1\"");

WriteLiteral(" role=\"dialog\"");

WriteLiteral(" aria-labelledby=\"XXXX\"");

WriteLiteral(" data-prefix=\"");

            
            #line 8 "..\..\Signum\Views\PopupControl.cshtml"
                                                                                                                                      Write(Model.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(">\r\n    <div");

WriteLiteral(" class=\"modal-dialog modal-lg\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"modal-content\"");

WriteLiteral(">\r\n            <div");

WriteLiteral(" class=\"modal-header\"");

WriteLiteral(">\r\n");

            
            #line 12 "..\..\Signum\Views\PopupControl.cshtml"
                
            
            #line default
            #line hidden
            
            #line 12 "..\..\Signum\Views\PopupControl.cshtml"
                 if(viewMode == ViewMode.Navigate)
                {

            
            #line default
            #line hidden
WriteLiteral("                <button");

WriteLiteral(" type=\"button\"");

WriteLiteral(" class=\"close sf-close-button\"");

WriteLiteral(">×</button>\r\n");

            
            #line 15 "..\..\Signum\Views\PopupControl.cshtml"
                }
                else
                {
                    var saveProtected = (bool)ViewData[ViewDataKeys.SaveProtected]; 

            
            #line default
            #line hidden
WriteLiteral("                    <div");

WriteLiteral(" style=\"float:right\"");

WriteLiteral(">\r\n                        <button");

WriteAttribute("id", Tuple.Create(" id=\"", 869), Tuple.Create("\"", 897)
            
            #line 20 "..\..\Signum\Views\PopupControl.cshtml"
, Tuple.Create(Tuple.Create("", 874), Tuple.Create<System.Object, System.Int32>(Model.Compose("btnOk")
            
            #line default
            #line hidden
, 874), false)
);

WriteAttribute("class", Tuple.Create(" class=\"", 898), Tuple.Create("\"", 1011)
, Tuple.Create(Tuple.Create("", 906), Tuple.Create("btn", 906), true)
, Tuple.Create(Tuple.Create(" ", 909), Tuple.Create("btn-primary", 910), true)
, Tuple.Create(Tuple.Create(" ", 921), Tuple.Create("sf-entity-button", 922), true)
, Tuple.Create(Tuple.Create(" ", 938), Tuple.Create("sf-close-button", 939), true)
, Tuple.Create(Tuple.Create(" ", 954), Tuple.Create("sf-ok-button", 955), true)
            
            #line 20 "..\..\Signum\Views\PopupControl.cshtml"
                                                   , Tuple.Create(Tuple.Create("", 967), Tuple.Create<System.Object, System.Int32>(saveProtected ? " sf-save-protected" : ""
            
            #line default
            #line hidden
, 967), false)
);

WriteLiteral(">");

            
            #line 20 "..\..\Signum\Views\PopupControl.cshtml"
                                                                                                                                                                          Write(JavascriptMessage.ok.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n                        <button");

WriteAttribute("id", Tuple.Create(" id=\"", 1091), Tuple.Create("\"", 1123)
            
            #line 21 "..\..\Signum\Views\PopupControl.cshtml"
, Tuple.Create(Tuple.Create("", 1096), Tuple.Create<System.Object, System.Int32>(Model.Compose("btnCancel")
            
            #line default
            #line hidden
, 1096), false)
);

WriteLiteral(" class=\"btn btn-default sf-entity-button sf-close-button sf-cancel-button\"");

WriteLiteral(">");

            
            #line 21 "..\..\Signum\Views\PopupControl.cshtml"
                                                                                                                                      Write(JavascriptMessage.cancel.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n                    </div>\r\n");

            
            #line 23 "..\..\Signum\Views\PopupControl.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("                <h4>\r\n                    <span");

WriteLiteral(" class=\"sf-entity-title\"");

WriteLiteral(">");

            
            #line 25 "..\..\Signum\Views\PopupControl.cshtml"
                                              Write(ViewBag.Title ?? Model.UntypedValue?.ToString());

            
            #line default
            #line hidden
WriteLiteral(" </span>\r\n");

            
            #line 26 "..\..\Signum\Views\PopupControl.cshtml"
                    
            
            #line default
            #line hidden
            
            #line 26 "..\..\Signum\Views\PopupControl.cshtml"
                      
                        var ident = Model.UntypedValue as Entity;

                        if (ident != null && !ident.IsNew && Navigator.IsNavigable(ident, null))
                        {

            
            #line default
            #line hidden
WriteLiteral("                        <a");

WriteAttribute("href", Tuple.Create(" href=\"", 1673), Tuple.Create("\"", 1711)
            
            #line 31 "..\..\Signum\Views\PopupControl.cshtml"
, Tuple.Create(Tuple.Create("", 1680), Tuple.Create<System.Object, System.Int32>(Navigator.NavigateRoute(ident)
            
            #line default
            #line hidden
, 1680), false)
);

WriteLiteral(" class=\"sf-popup-fullscreen\"");

WriteLiteral(">\r\n                            <span");

WriteLiteral(" class=\"glyphicon glyphicon-new-window\"");

WriteLiteral("></span>\r\n                        </a>\r\n");

            
            #line 34 "..\..\Signum\Views\PopupControl.cshtml"
                        }
                    
            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n                    <br />\r\n                    <small>");

            
            #line 38 "..\..\Signum\Views\PopupControl.cshtml"
                      Write(Navigator.Manager.GetTypeTitle(modifiable));

            
            #line default
            #line hidden
WriteLiteral("</small>\r\n                </h4>\r\n            </div>\r\n            <div");

WriteLiteral(" class=\"modal-body\"");

WriteLiteral(">\r\n                <div");

WriteLiteral(" class=\"sf-button-bar\"");

WriteLiteral(">\r\n");

WriteLiteral("                    ");

            
            #line 43 "..\..\Signum\Views\PopupControl.cshtml"
               Write(ButtonBarEntityHelper.GetForEntity(new EntityButtonContext
                    {
                        Url = Url,
                        ViewMode = viewMode,
                        ControllerContext = this.ViewContext,
                        PartialViewName = ViewData[ViewDataKeys.PartialViewName].ToString(),
                        Prefix = Model.Prefix,
                        ShowOperations = (bool?)ViewData[ViewDataKeys.ShowOperations] ?? true,
                    }, modifiable).ToStringButton(Html));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n\r\n");

WriteLiteral("                ");

            
            #line 54 "..\..\Signum\Views\PopupControl.cshtml"
           Write(Html.ValidationSummaryAjax(Model));

            
            #line default
            #line hidden
WriteLiteral("\r\n                <div");

WriteAttribute("id", Tuple.Create(" id=\"", 2783), Tuple.Create("\"", 2820)
            
            #line 55 "..\..\Signum\Views\PopupControl.cshtml"
, Tuple.Create(Tuple.Create("", 2788), Tuple.Create<System.Object, System.Int32>(Model.Compose("divMainControl")
            
            #line default
            #line hidden
, 2788), false)
);

WriteAttribute("class", Tuple.Create(" class=\"", 2821), Tuple.Create("\"", 2910)
, Tuple.Create(Tuple.Create("", 2829), Tuple.Create("sf-main-control", 2829), true)
, Tuple.Create(Tuple.Create(" ", 2844), Tuple.Create("form-horizontal", 2845), true)
            
            #line 55 "..\..\Signum\Views\PopupControl.cshtml"
                  , Tuple.Create(Tuple.Create("", 2860), Tuple.Create<System.Object, System.Int32>(modifiable.IsGraphModified ? " sf-changed" : ""
            
            #line default
            #line hidden
, 2860), false)
);

WriteLiteral(" \r\n        data-prefix=\"");

            
            #line 56 "..\..\Signum\Views\PopupControl.cshtml"
                Write(Model.Prefix);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(" \r\n        data-runtimeinfo=\"");

            
            #line 57 "..\..\Signum\Views\PopupControl.cshtml"
                     Write(Model.RuntimeInfo().ToString());

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral("\r\n        data-test-ticks=\"");

            
            #line 58 "..\..\Signum\Views\PopupControl.cshtml"
                    Write(DateTime.Now.Ticks);

            
            #line default
            #line hidden
WriteLiteral("\"");

WriteLiteral(">\r\n");

            
            #line 59 "..\..\Signum\Views\PopupControl.cshtml"
                    
            
            #line default
            #line hidden
            
            #line 59 "..\..\Signum\Views\PopupControl.cshtml"
                     if (((bool?)ViewData[ViewDataKeys.WriteEntityState]) == true)
                    {
                        
            
            #line default
            #line hidden
            
            #line 61 "..\..\Signum\Views\PopupControl.cshtml"
                   Write(Html.Hidden(Model.Compose(ViewDataKeys.EntityState), Navigator.Manager.SerializeEntity(modifiable)));

            
            #line default
            #line hidden
            
            #line 61 "..\..\Signum\Views\PopupControl.cshtml"
                                                                                                                            
                    }

            
            #line default
            #line hidden
WriteLiteral("                    ");

            
            #line 63 "..\..\Signum\Views\PopupControl.cshtml"
                       
                        ViewData[ViewDataKeys.InPopup] = true;

                        Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString(), Model);
                    
            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n            </div>\r\n        </div>\r\n    </div>\r\n</div>\r" +
"\n");

        }
    }
}
#pragma warning restore 1591
