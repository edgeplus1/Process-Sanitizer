using System;
using System.Collections.Generic;
using System.Text;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Экспорт отчёта: TXT и HTML.
    /// </summary>
    public static class ReportExporter
    {
        public static string BuildTxt(List<ProcessInfo> procs, List<QuarantineItem> quar, List<PersistenceEntry> pers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==========================================================");
            sb.AppendLine(" Process Sanitizer — отчёт о состоянии системы");
            sb.AppendLine(" Дата: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine(" Машина: " + Environment.MachineName +
                          "   Пользователь: " + Environment.UserDomainName + "\\" + Environment.UserName);
            sb.AppendLine("==========================================================");
            sb.AppendLine();
            sb.AppendLine("Сводка: процессов " + procs.Count +
                          ", угроз " + CountThreats(procs) +
                          ", в карантине " + quar.Count +
                          ", автозагрузок " + pers.Count +
                          " (подозрительных " + CountSusp(pers) + ")");
            sb.AppendLine();

            sb.AppendLine("--- УГРОЗЫ (HIGH / CRITICAL) ---");
            foreach (ProcessInfo pi in procs)
            {
                if (pi.Risk != "HIGH" && pi.Risk != "CRITICAL") continue;
                sb.AppendLine("[" + pi.Risk + "] " + pi.Name + " (PID " + pi.Pid + ", баллов " + pi.RiskScore + ")");
                sb.AppendLine("    путь: " + pi.Path);
                sb.AppendLine("    подпись: " + pi.Signature);
                foreach (string r in pi.RiskReasons) sb.AppendLine("    " + r);
                sb.AppendLine();
            }

            sb.AppendLine("--- ВСЕ ПРОЦЕССЫ ---");
            foreach (ProcessInfo pi in procs)
                sb.AppendLine(pi.Risk + " | " + pi.Name + " | PID " + pi.Pid + " | " + pi.User +
                              " | баллов " + pi.RiskScore + " | " + pi.Signature + " | " + pi.Path);
            sb.AppendLine();

            sb.AppendLine("--- КАРАНТИН ---");
            foreach (QuarantineItem it in quar)
                sb.AppendLine(it.Id + " | " + it.AddedAt.ToString("yyyy-MM-dd HH:mm:ss") +
                              " | " + it.Sha256 + " | " + it.OriginalPath);
            sb.AppendLine();

            sb.AppendLine("--- АВТОЗАГРУЗКИ И ЗАКРЕПЛЕНИЯ ---");
            foreach (PersistenceEntry en in pers)
                sb.AppendLine((en.Suspicious ? "[ПРОВЕРИТЬ] " : "[ок] ") + en.Location +
                              " | " + en.Name + " | " + en.Command +
                              (en.Reason.Length > 0 ? " | " + en.Reason : ""));
            return sb.ToString();
        }

        public static string BuildHtml(List<ProcessInfo> procs, List<QuarantineItem> quar, List<PersistenceEntry> pers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<title>Process Sanitizer — отчёт</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;background:#fafafa;color:#222}");
            sb.AppendLine("h1{font-size:22px}h2{font-size:16px;margin-top:28px;border-bottom:1px solid #ddd;padding-bottom:4px}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;font-size:12px}");
            sb.AppendLine("td,th{border:1px solid #ddd;padding:4px 8px;text-align:left;vertical-align:top}");
            sb.AppendLine("th{background:#f0f0f0}");
            sb.AppendLine(".CRITICAL{background:#ffd7dc;color:#8c0000}.HIGH{background:#ffe0c8;color:#963c00}.MEDIUM{background:#fff6d6}");
            sb.AppendLine(".meta{color:#666;font-size:12px}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h1>Process Sanitizer — отчёт о состоянии системы</h1>");
            sb.AppendLine("<p class=\"meta\">" + E(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                          " | " + E(Environment.MachineName) +
                          " | " + E(Environment.UserDomainName + "\\" + Environment.UserName) + "</p>");
            sb.AppendLine("<p>Процессов: <b>" + procs.Count + "</b> | Угроз: <b>" + CountThreats(procs) +
                          "</b> | В карантине: <b>" + quar.Count + "</b> | Автозагрузок: <b>" + pers.Count +
                          "</b> (подозрительных " + CountSusp(pers) + ")</p>");

            sb.AppendLine("<h2>Угрозы (HIGH / CRITICAL)</h2>");
            sb.AppendLine("<table><tr><th>Риск</th><th>Процесс</th><th>PID</th><th>Баллы</th><th>Путь</th><th>Подпись</th><th>Причины</th></tr>");
            foreach (ProcessInfo pi in procs)
            {
                if (pi.Risk != "HIGH" && pi.Risk != "CRITICAL") continue;
                sb.AppendLine("<tr class=\"" + pi.Risk + "\"><td>" + pi.Risk + "</td><td>" + E(pi.Name) +
                              "</td><td>" + pi.Pid + "</td><td>" + pi.RiskScore + "</td><td>" + E(pi.Path) +
                              "</td><td>" + E(pi.Signature) + "</td><td>" + E(string.Join("; ", pi.RiskReasons)) + "</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Все процессы</h2>");
            sb.AppendLine("<table><tr><th>Риск</th><th>Процесс</th><th>PID</th><th>Пользователь</th><th>CPU %</th><th>Память МБ</th><th>Подпись</th><th>Путь</th></tr>");
            foreach (ProcessInfo pi in procs)
            {
                sb.AppendLine("<tr class=\"" + pi.Risk + "\"><td>" + pi.Risk + "</td><td>" + E(pi.Name) +
                              "</td><td>" + pi.Pid + "</td><td>" + E(pi.User) +
                              "</td><td>" + (pi.CpuPercent < 0 ? "—" : pi.CpuPercent.ToString("0.0")) +
                              "</td><td>" + (pi.MemoryMb < 0 ? "—" : pi.MemoryMb.ToString("0")) +
                              "</td><td>" + E(pi.Signature) + "</td><td>" + E(pi.Path) + "</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Карантин</h2>");
            sb.AppendLine("<table><tr><th>ID</th><th>Помещён</th><th>SHA256</th><th>Исходный путь</th></tr>");
            foreach (QuarantineItem it in quar)
                sb.AppendLine("<tr><td>" + E(it.Id) + "</td><td>" + it.AddedAt.ToString("yyyy-MM-dd HH:mm:ss") +
                              "</td><td>" + E(it.Sha256) + "</td><td>" + E(it.OriginalPath) + "</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Автозагрузки и закрепления</h2>");
            sb.AppendLine("<table><tr><th>Вердикт</th><th>Где</th><th>Имя</th><th>Команда</th><th>Причина</th></tr>");
            foreach (PersistenceEntry en in pers)
            {
                string cls = en.Suspicious ? " class=\"HIGH\"" : "";
                sb.AppendLine("<tr" + cls + "><td>" + (en.Suspicious ? "ПРОВЕРИТЬ" : "ок") + "</td><td>" + E(en.Location) +
                              "</td><td>" + E(en.Name) + "</td><td>" + E(en.Command) + "</td><td>" + E(en.Reason) + "</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static int CountThreats(List<ProcessInfo> procs)
        {
            int n = 0;
            foreach (ProcessInfo pi in procs)
                if (pi.Risk == "HIGH" || pi.Risk == "CRITICAL") n++;
            return n;
        }

        private static int CountSusp(List<PersistenceEntry> pers)
        {
            int n = 0;
            foreach (PersistenceEntry en in pers)
                if (en.Suspicious) n++;
            return n;
        }

        private static string E(string s)
        {
            return System.Net.WebUtility.HtmlEncode(s ?? "");
        }
    }
}