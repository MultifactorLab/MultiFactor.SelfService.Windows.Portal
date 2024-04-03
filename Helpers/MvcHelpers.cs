using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace MultiFactor.SelfService.Windows.Portal.Helpers
{
    public static class MvcHelpers
    {
        public static MvcHtmlString ToggablePasswordFor<TModel, TProperty>(
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
                            <img class=""showpassword-img"" src=""/Content/images/show.svg"" />
                            <img class=""hidepassword-img"" src=""/Content/images/hide.svg"" style=""display: none"" />
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