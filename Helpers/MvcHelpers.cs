using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace MultiFactor.SelfService.Windows.Portal.Helpers
{
    public static class MvcHelpers
    {
        public static MvcHtmlString ToggleablePasswordFor<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes)
        {
            var passwordField = htmlHelper.PasswordFor(expression, htmlAttributes);
            var idRegexp = new Regex("id=\"?(?<id>[a-zA-Z0-9]+)\"?");
            var id = idRegexp.Match(passwordField.ToString()).Groups["id"].Value;

            if (string.IsNullOrEmpty(id))
            {
                return passwordField;
            }
            string markup = $@"
                 <div class=""input showpassword-container {id}"">
                        <a class=""showpassword-switch"">
                            <span class=""showpassword-img"">
                                <svg version=""1.1"" id=""Capa_1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" x=""0px"" y=""0px"" viewBox=""0 0 409.6 409.6"" style=""enable-background:new 0 0 409.6 409.6;"" xml:space=""preserve""><g><g><path d=""M204.8,68.268c-87.567,0-163.835,54.999-204.8,136.535c40.965,81.551,117.233,136.53,204.8,136.53 s163.835-54.979,204.8-136.53C368.635,123.272,292.367,68.268,204.8,68.268z M204.8,307.203c-65.9,0-128.133-38.82-165.914-102.4 c37.78-63.58,100.014-102.4,165.914-102.4c65.899,0,128.133,38.82,165.914,102.4C332.933,268.383,270.699,307.203,204.8,307.203z""></path></g></g><g><g><path d=""M204.8,128.003c-42.435,0-76.8,34.365-76.8,76.8c0,42.419,34.365,76.8,76.8,76.8c42.414,0,76.8-34.381,76.8-76.8 C281.6,162.368,247.214,128.003,204.8,128.003z M204.8,247.473c-23.567,0-42.665-19.098-42.665-42.67 c0-23.567,19.098-42.665,42.665-42.665s42.665,19.098,42.665,42.665C247.465,228.375,228.367,247.473,204.8,247.473z""></path></g></g></svg>
                            </span>
                            <span class=""hidepassword-img"" style=""display: none"">
                                <svg version=""1.1"" id=""Capa_1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" x=""0px"" y=""0px"" viewBox=""0 0 512 512"" style=""enable-background:new 0 0 512 512;"" xml:space=""preserve""><g><g><path d=""M244.488,329.053c-31.578-4.957-56.584-29.965-61.541-61.541l-37.839-37.839c-2.002,8.521-3.038,17.338-3.038,26.327 c0,62.821,51.108,113.93,113.93,113.93c8.97,0,17.787-1.04,26.315-3.05L244.488,329.053z""></path></g></g><g><g><path d=""M496.046,279.068c2.728-3.56,7.791-11.018,8.359-11.858L512,255.998l-7.596-11.211c-0.569-0.839-5.63-8.295-8.358-11.854 c-12.74-16.618-42.574-55.535-83.705-89.578C360.834,100.725,308.234,79.109,256,79.109c-37.987,0-76.378,11.535-114.325,34.299 L28.782,0.516L0.516,28.782l482.702,482.702l28.267-28.267L403.773,375.506C449.51,339.752,482.616,296.586,496.046,279.068z M320.486,292.22l-26.689-26.689c0.767-3.05,1.178-6.242,1.178-9.531c0-21.526-17.45-38.976-38.976-38.976 c-3.288,0-6.48,0.412-9.53,1.178l-26.688-26.688c10.954-6.168,23.36-9.469,36.218-9.469c40.779,0,73.954,33.176,73.954,73.954 C329.954,268.884,326.652,281.275,320.486,292.22z M349.46,321.193c13.299-18.989,20.47-41.563,20.47-65.193 c0-62.821-51.108-113.93-113.93-113.93c-23.579,0-46.181,7.184-65.185,20.478l-19.858-19.858 c28.714-15.674,57.235-23.606,85.042-23.606c67.635,0,137.397,46.067,207.357,136.917c-14.551,18.969-45.554,58.882-88.078,91.012 L349.46,321.193z""></path></g></g><g><g><path d=""M300.958,386.19c-15.135,4.469-30.147,6.726-44.958,6.726c-67.634,0-137.396-46.067-207.357-136.915 c10.041-13.088,30.201-39.06,57.341-64.785l-28.278-28.278c-29.893,28.457-51.502,56.629-61.752,69.998 c-2.727,3.559-7.788,11.014-8.357,11.854L0,255.999l7.595,11.212c0.568,0.838,5.63,8.297,8.359,11.858 c12.74,16.618,42.575,55.534,83.705,89.577c51.506,42.631,104.107,64.245,156.341,64.245c25.353,0,50.884-5.137,76.342-15.318 L300.958,386.19z""></path></g></g></svg>
                            </span>
                        </a>
                        {passwordField}
                </div>

            ";
            string script = @"<script>
                $(function() {
                     $('." + id + @" .showpassword-switch').click(function () {
                        let input = $('#" + id + @"');
                        if (input.attr('type') == 'password') {
                            input.attr('type', 'text');
                        } else {
                            input.attr('type', 'password');
                        }
                        $('." + id + @" .showpassword-img').toggle();
                        $('." + id + @" .hidepassword-img').toggle();
                    })
                })
            </script>";

            // Concatenate the password field, the toggle button, and the script
            return MvcHtmlString.Create(markup + script);
        }
    }

}