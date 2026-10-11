using System.Collections;
using static StoryTools;

// Chapter 2, beat 5: report Ghostline on the "Report a Cybercrime" page with the clues the player found.
// Like a real report, nothing happens right away: the page only confirms it was received.
// What the report leads to is revealed later, in the epilogue (BedtimeBeat).
// Pages: LaptopCanvas > NightApps > Browser > ReportPage, ReportSentPage.
public static class FinaleBeat
{
    public static IEnumerator Play()
    {
        var laptop = Laptop;

        // The seller clue only shows if the player opened the scam seller's chat.
        laptop.sellerClue.SetActive(Progress.sawGhostlineSeller);
        laptop.OpenPage(laptop.reportPage, "Report a Cybercrime");
        yield return WaitForLaptopChoice("send");

        laptop.OpenPage(laptop.reportSentPage, "Report a Cybercrime > Received");
        CompleteObjective(2);
        ConsequenceToast.Show();
        yield return Wait(3f);
        Say("Done. I hope they actually catch them.", 3.5f);
    }
}
