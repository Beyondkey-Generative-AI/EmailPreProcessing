using HtmlAgilityPack;
using System.Text.RegularExpressions;


namespace EmailManagementAPI
{
    public static class Utility
    {
        public static string TruncateHtmlString(string input, int maxLength)
        {
            try
            {
                if (input.Length <= maxLength)
                {
                    return input;
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(input);

                string truncatedHtml = TruncateHtmlNode(doc.DocumentNode, maxLength).OuterHtml;

                return truncatedHtml;
            }
            catch
            {
                return "err";
            }
        }

        private static HtmlNode TruncateHtmlNode(HtmlNode node, int maxLength)
        {
            HtmlNode truncatedNode = node.CloneNode(false);

            foreach (var child in node.ChildNodes)
            {
                if (maxLength <= 0)
                {
                    break;
                }

                if (child.NodeType == HtmlNodeType.Text)
                {
                    string text = child.InnerHtml;
                    int length = Math.Min(text.Length, maxLength);

                    truncatedNode.InnerHtml += text.Substring(0, length);
                    maxLength -= length;
                }
                else
                {
                    HtmlNode truncatedChild = TruncateHtmlNode(child, maxLength);
                    truncatedNode.AppendChild(truncatedChild);
                    maxLength -= truncatedChild.OuterHtml.Length;
                }
            }

            return truncatedNode;
        }

        public static string MaskSensitiveInformation(string input)
        {
            try
            {
                // Define patterns to match sensitive information (e.g., phone numbers and email addresses)
                string phoneNumberPattern = @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b"; // Matches common phone number formats
                string emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";

                // Replace sensitive information with a masking string (e.g., "[PHONE]" and "[EMAIL]")
                string maskedInput = Regex.Replace(input, phoneNumberPattern, "[PHONE]");
                maskedInput = Regex.Replace(maskedInput, emailPattern, "[EMAIL]");

                return maskedInput;
            }
            catch { return "err"; }
        }
    }
}
