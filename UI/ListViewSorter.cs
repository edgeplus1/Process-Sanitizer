using System;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Сортировка ListView по колонке.
    /// PID — как число, CPU/память — как число, риск — по severity, остальное — строкой.
    /// </summary>
    public class ListViewSorter : IComparer
    {
        public int Column = 0;
        public SortOrder Order = SortOrder.Ascending;

        public int Compare(object x, object y)
        {
            ListViewItem a = (ListViewItem)x;
            ListViewItem b = (ListViewItem)y;
            string sa = a.SubItems[Column].Text;
            string sb = b.SubItems[Column].Text;

            int result;
            if (Column == 1) result = CompareInt(sa, sb);
            else if (Column == 4 || Column == 5) result = CompareDouble(sa, sb);
            else if (Column == 6) result = CompareRisk(sa, sb);
            else result = string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);

            if (Order == SortOrder.Descending) result = -result;
            return result;
        }

        private static int CompareInt(string a, string b)
        {
            int va, vb;
            if (!int.TryParse(a, out va)) va = -1;
            if (!int.TryParse(b, out vb)) vb = -1;
            return va.CompareTo(vb);
        }

        private static int CompareDouble(string a, string b)
        {
            double va, vb;
            if (!double.TryParse(a, NumberStyles.Any, CultureInfo.CurrentCulture, out va)) va = -1;
            if (!double.TryParse(b, NumberStyles.Any, CultureInfo.CurrentCulture, out vb)) vb = -1;
            return va.CompareTo(vb);
        }

        private static int CompareRisk(string a, string b)
        {
            return RiskRank(a).CompareTo(RiskRank(b));
        }

        private static int RiskRank(string s)
        {
            switch (s)
            {
                case "SAFE": return 0;
                case "LOW": return 1;
                case "MEDIUM": return 2;
                case "HIGH": return 3;
                case "CRITICAL": return 4;
                default: return -1;
            }
        }
    }
}