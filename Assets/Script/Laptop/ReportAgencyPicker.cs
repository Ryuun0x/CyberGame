using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Report page: the player picks who receives the report. Each tab shows that agency's real contact details,
// and the "report received" page says who got it. Numbers checked in November 2026: they do change, so check them
// again before showing the game outside class.
// On LaptopCanvas > NightApps > Browser > ReportPage; the tabs and text are under its ContactsBox.
public class ReportAgencyPicker : MonoBehaviour
{
    public struct Agency { public string name, site, contacts, urgent; }

    public static readonly Agency[] Agencies =
    {
        new Agency
        {
            name = "the CICC", site = "cicc.gov.ph",
            contacts = "<b>CICC</b>  <color=#BFD2F5>Cybercrime Investigation\nand Coordinating Center</color>\n" +
                       "Hotline <b><size=130%>1326</size></b>  <color=#BFD2F5>(24/7, toll-free)</color>\n\n" +
                       "<b>If 1326 is busy:</b>\nSmart  0947-714-7105\nGlobe  0966-976-5971\nDITO   0991-481-4225\n\n" +
                       "<b>Email:</b>  report@cicc.gov.ph",
            urgent = "For urgent cases, call the CICC hotline 1326 (24/7) or email report@cicc.gov.ph.",
        },
        new Agency
        {
            name = "the PNP Anti-Cybercrime Group", site = "acg.pnp.gov.ph",
            contacts = "<b>PNP Anti-Cybercrime Group</b>\n<color=#BFD2F5>Philippine National Police</color>\n\n" +
                       "Hotline <b>(02) 8723-0401</b>\nlocal <b>7491</b>\n\n" +
                       "<b>Website:</b>  acg.pnp.gov.ph\n<b>Walk in:</b>  Camp Crame, Quezon City\n\n" +
                       "<color=#BFD2F5>To file a criminal complaint with the police.</color>",
            urgent = "For urgent cases, call the PNP Anti-Cybercrime Group at (02) 8723-0401 local 7491, or the CICC hotline 1326 (24/7).",
        },
        new Agency
        {
            name = "the NBI Cybercrime Division", site = "nbi.gov.ph",
            contacts = "<b>NBI Cybercrime Division</b>\n<color=#BFD2F5>National Bureau of Investigation</color>\n\n" +
                       "Trunk line <b>(02) 8523-8231</b> to 38\nlocal <b>3455</b>\n\n" +
                       "<b>Email:</b>  ccd@nbi.gov.ph\n<b>Office hours:</b>  Mon-Fri, 8 AM-5 PM\n\n" +
                       "<color=#BFD2F5>To have the NBI investigate and file a case.</color>",
            urgent = "For urgent cases, call the NBI at (02) 8523-8231 local 3455, or the CICC hotline 1326 (24/7).",
        },
    };

    [Tooltip("The tabs, same order as Agencies: CICC, PNP-ACG, NBI.")]
    public Button[] tabs;
    [Tooltip("ContactsBox > Contacts: the picked agency's numbers.")]
    public TextMeshProUGUI contacts;
    [Tooltip("ReportSentPage > Body: says who received the report.")]
    public TextMeshProUGUI sentBody;
    public LaptopNight laptop;
    public Color picked = new Color(0.235f, 0.49f, 1f), notPicked = new Color(0.1f, 0.2f, 0.4f);

    public static int Chosen { get; private set; }
    public static string Address => Agencies[Chosen].site + "/report";

    void Awake()
    {
        Chosen = 0; // a new game starts on the CICC
        for (int i = 0; i < tabs.Length; i++)
        {
            int n = i;
            tabs[i].onClick.AddListener(() => Pick(n));
        }
    }

    void OnEnable() => Pick(Chosen);

    public void Pick(int i)
    {
        Chosen = i;
        var a = Agencies[i];
        contacts.text = a.contacts;
        sentBody.text = $"Thank you. Your report was sent to {a.name}. An investigator will review it and may contact you. " +
                        $"This can take several weeks.\n\n{a.urgent}";
        if (laptop.reportPage.activeInHierarchy) laptop.addressText.text = Address;
        for (int j = 0; j < tabs.Length; j++) tabs[j].image.color = j == i ? picked : notPicked;
    }
}
