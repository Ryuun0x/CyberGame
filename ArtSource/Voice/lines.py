"""Every voiced line in the game, for make_voice.py.
Each entry: (text shown in the game, speaker, how, what Chatterbox reads)
  text      exactly as the subtitle shows it (the game finds the voice file by this text); rich-text tags are ignored
  speaker   alice / news / barista: the voice is ArtSource/Voice/refs/<speaker>.wav (built-in voice if missing)
  how       "said": out loud, dry and present
            "thought": Alice's inner voice, softer and closer, with a faint airy space around it (make_voice.py)
  read      the words with Chatterbox-Turbo tags for the emotion of the moment ([sigh] [gasp] [groan] [fear]
            [surprised] [whispering] [angry] [crying] [happy] [sarcastic] [dramatic] [narration] [sniff] ...).
            Key hints in the subtitle ("(Tab)", "laptop > Browser > ...") are left out of what is read.
Not voiced: Kai's texts (they are phone messages), captions like "Two weeks later...", on-screen UI.
A line the game shows without a voice file is logged in the Editor ("[Voice] no line for ..."), so it can be added here.
"""

LINES = [
    # ---- Prologue: the night before (Prologue.cs) ------------------------------------------------------------
    ("Wait, what?? Are you okay?", "alice", "said", "[surprised] Wait, what?? [fear] Are you okay?"),
    ("...a wave of online scams is targeting students across the city tonight.", "news", "said",
     "[dramatic] A wave of online scams is targeting students across the city, tonight."),  # a professional anchor:
    ("Victims report fake \"free Wi-Fi\", urgent texts, and messages from hacked friends' accounts.", "news", "said",
     "[dramatic] Victims report fake free Wi-Fi, urgent texts, and messages from hacked friends' accounts."),  # serious and
    ("Police link the attacks to a group calling itself Ghostline.", "news", "said",
     "[dramatic] Police link the attacks to a group calling itself... Ghostline."),  # concerned, but composed (no sighs)
    ("Ghostline... I hope Kai's okay.", "alice", "said", "[sigh] Ghostline... I hope Kai's okay."),

    # ---- Wake-up, the morning after (Prologue.PlayWakeUp, WakeUpSequence) ---------------------------------------
    ("Ugh... my head.", "alice", "said", "[groan] Ugh... my head."),
    ("Kai... I hope they're okay.", "alice", "said", "[sigh] Kai... I hope they're okay."),
    ("Wait, it's already nine?! The thesis is due at noon.", "alice", "said",
     "[gasp] Wait, it's already nine?! [fear] The thesis is due at noon."),
    ("Wait, it's already nine?! The thesis is due at noon. I need to submit it now.", "alice", "said",
     "[gasp] Wait, it's already nine?! [fear] The thesis is due at noon. I need to submit it now."),
    ("Ugh... Thesis is due at noon. I better check if the file synced.", "alice", "thought",
     "[groan] Ugh... [sigh] Thesis is due at noon. I better check if the file synced."),

    # ---- Chapter 1 at home: the laptop, the backup, 2FA -------------------------------------------------------
    ("What?! No internet? You've got to be kidding me!", "alice", "said",
     "[surprised] What?! No internet? [angry] You've got to be kidding me!"),
    ("No internet, so it can't upload. I'll save it to my Drive once I'm online.", "alice", "thought",
     "[sigh] No internet, so it can't upload. I'll save it to my Drive once I'm online."),
    ("I need to find a flash drive to back this up...", "alice", "thought", "I need to find a flash drive to back this up..."),
    ("I think I left one in the living room.", "alice", "thought", "Hmm... I think I left one in the living room."),
    ("Found a flash drive.", "alice", "thought", "[happy] Found a flash drive."),
    ("I can use this to backup my thesis.", "alice", "thought", "I can use this to back up my thesis."),
    ("Phew, thesis is backed up.", "alice", "thought", "[sigh] Phew, thesis is backed up."),
    ("Thesis is backed up.", "alice", "thought", "Thesis is backed up."),
    ("I could turn on 2FA on my phone too, before heading out.", "alice", "thought",
     "I could turn on two F A on my phone too, before heading out."),
    ("2FA is on.", "alice", "thought", "[happy] Two F A is on."),
    ("Okay, everything's secure.", "alice", "thought", "[happy] Okay, everything's secure."),
    ("Now I just need to head out to the cafe to find some wifi and send this off.", "alice", "thought",
     "Now I just need to head out to the café to find some wifi and send this off."),
    ("I still need to back up my thesis on the laptop before I go anywhere.", "alice", "thought",
     "I still need to back up my thesis on the laptop before I go anywhere."),
    ("I should eject it first, or the files could get corrupted.", "alice", "thought",
     "I should eject it first, or the files could get corrupted."),
    ("I should eject and unplug the flash drive first.", "alice", "thought", "I should eject and unplug the flash drive first."),
    ("I should bring my laptop with me.", "alice", "thought", "I should bring my laptop with me."),
    ("I don't need my phone right now. Let me focus on my thesis.", "alice", "thought",
     "I don't need my phone right now. [sigh] Let me focus on my thesis."),
    ("I can't do that right now.", "alice", "thought", "I can't do that right now."),
    ("I already submitted my thesis.", "alice", "thought", "I already submitted my thesis."),

    # ---- Chapter 1 in the city and the café -------------------------------------------------------------------
    ("I need to find a café with Wi-Fi to send my thesis.", "alice", "thought",
     "I need to find a café with Wi-Fi to send my thesis."),
    ("One iced latte, please.", "alice", "said", "[happy] One iced latte, please."),
    ("Barista: Coming right up! It'll be a few minutes.", "barista", "said", "[happy] Coming right up! It'll be a few minutes."),
    ("Barista: Iced latte for Alice! The Wi-Fi password's on your receipt.", "barista", "said",
     "[happy] Iced latte for Alice! The Wi-Fi password's on your receipt."),
    ("I should order something at the counter first before using their Wi-Fi.", "alice", "thought",
     "I should order something at the counter first before using their Wi-Fi."),
    ("I'm not connected to the internet.", "alice", "thought", "[sigh] I'm not connected to the internet."),
    ("I need to get this laptop on the café Wi-Fi first.", "alice", "thought", "I need to get this laptop on the café Wi-Fi first."),
    ("Uploading... Done!", "alice", "thought", "Uploading... [happy] Done!"),
    ("Perfect. The thesis is submitted securely.", "alice", "thought", "[happy] Perfect. The thesis is submitted securely."),
    ("Looks like I'm finally done! Time to head out.", "alice", "thought", "[happy] Looks like I'm finally done! Time to head out."),
    ("…Kai said not to trust messages from them.", "alice", "thought", "[fear] Kai said not to trust messages from them..."),
    ("I'll deal with this when I get home.", "alice", "thought", "[sigh] I'll deal with this when I get home."),
    ("It's too late to go out. I'm staying in tonight.", "alice", "thought", "[sigh] It's too late to go out. I'm staying in tonight."),

    # ---- Chapter 2: the night ---------------------------------------------------------------------------------
    ("It's late... I can't sleep. Big exam tomorrow too.", "alice", "thought",
     "[sigh] It's late... I can't sleep. Big exam tomorrow too."),
    ("Right, the exam. I still need a scientific calculator... I'll look for one on my laptop.", "alice", "thought",
     "Right, the exam. I still need a scientific calculator... I'll look for one on my laptop."),
    ("I should buy that calculator. My laptop's on the desk.", "alice", "thought", "I should buy that calculator. My laptop's on the desk."),
    ("On the laptop: open the Browser, then click ShopHub.", "alice", "thought", "On the laptop... open the browser, then ShopHub."),
    ("Same password as always. Easier to remember.", "alice", "thought", "Same password as always. [chuckle] Easier to remember."),
    ("Okay, the calculator's sorted.", "alice", "thought", "[happy] Okay, the calculator's sorted."),
    ("Wait... that profile picture. It's the same ghost I saw on that stranger's laptop at the café.", "alice", "thought",
     "Wait... [fear] that profile picture. It's the same ghost I saw on that stranger's laptop at the café."),
    ("My phone's buzzing... (press Esc to close the laptop)", "alice", "thought", "My phone's buzzing..."),
    ("My phone keeps buzzing... (Tab)", "alice", "thought", "My phone keeps buzzing..."),
    ("There. I hope Kai's okay...", "alice", "thought", "[sigh] There. I hope Kai's okay..."),
    ("So it really was a scammer. Good thing I called.", "alice", "thought", "[sigh] So it really was a scammer. Good thing I called."),
    ("That wasn't me... I should change my password.", "alice", "thought", "[fear] That wasn't me... I should change my password."),
    ("Someone changed my password... At least I got my account back.", "alice", "thought",
     "[fear] Someone changed my password... [sigh] At least I got my account back."),
    ("Same password as my Goggle account...", "alice", "thought", "[fear] Same password as my Goggle account..."),
    ("They got into my Goggle account with my ShopHub password...", "alice", "thought",
     "[fear] They got into my Goggle account with my ShopHub password..."),
    ("2FA stopped them. I'm changing that password now.", "alice", "thought",
     "[sigh] Two F A... stopped them. I'm changing that password now."),
    ("Oh no... I let them in.", "alice", "said", "[gasp] Oh no... [crying] I let them in."),
    ("I paid... and they just want more?!", "alice", "said", "[angry] I paid... and they just want more?!"),
    ("Good thing I had a backup on the flash drive.", "alice", "thought", "[sigh] Good thing I had a backup on the flash drive."),
    ("Good thing I had a backup on my Drive.", "alice", "thought", "[sigh] Good thing I had a backup on my Drive."),
    ("Everything's gone... I should have kept a backup.", "alice", "said", "[crying] Everything's gone... I should have kept a backup."),
    ("The ghost logo again... Ghostline is behind all of this. I should report them.", "alice", "thought",
     "[fear] The ghost logo again... Ghostline is behind all of this. [angry] I should report them."),
    ("The fake Wi-Fi, Kai's hacked account... that stranger with the Ghostline logo. I should report them.", "alice", "thought",
     "The fake Wi-Fi, Kai's hacked account... that stranger with the Ghostline logo. [angry] I should report them."),
    ("I need to report Ghostline: laptop > Browser > Report a Cybercrime.", "alice", "thought",
     "I need to report Ghostline. The browser... Report a Cybercrime."),
    ("Done. I hope they actually catch them.", "alice", "thought", "[sigh] Done. I hope they actually catch them."),
    ("I should get some sleep. (Esc to close the laptop, then press E on the bed)", "alice", "thought",
     "[sigh] I should get some sleep."),
    ("It's really late. I should get some sleep. (Esc to close the laptop, then press E on the bed)", "alice", "thought",
     "[sigh] It's really late. I should get some sleep."),
    ("What a day... At least I reported them.", "alice", "thought", "[sigh] What a day... At least I reported them."),

    # ---- Epilogue, two weeks later (BedtimeBeat.EpilogueLines) ----------------------------------------------------
    ("The police took down Ghostline's accounts. My report connected the fake Wi-Fi, the hacked accounts and the fake shop.",
     "alice", "thought",
     "[happy] The police took down Ghostline's accounts. My report connected the fake Wi-Fi, the hacked accounts and the fake shop."),
    ("Ghostline's accounts were taken down after the reports.", "alice", "thought", "Ghostline's accounts were taken down after the reports."),
    ("Nothing came of the report. And yesterday a new \"free Wi-Fi\" network showed up at another café...", "alice", "thought",
     "[sigh] Nothing came of the report. [fear] And yesterday a new free Wi-Fi network showed up at another café..."),
    ("Kai finally got their account back.", "alice", "thought", "[happy] Kai finally got their account back."),
    ("Kai never got my ₱1,000. It went straight to Ghostline.", "alice", "thought",
     "[sigh] Kai never got my one thousand pesos. It went straight to Ghostline."),
    ("The calculator from gadget.deals.ph never arrived.", "alice", "thought",
     "[sarcastic] The calculator from gadget dot deals dot P H never arrived."),
    ("Ghostline used my Goggle account to message my classmates before I got it back.", "alice", "thought",
     "[sigh] Ghostline used my Goggle account to message my classmates before I got it back."),
    ("I never got back the files on my old laptop.", "alice", "thought", "[sigh] I never got back the files on my old laptop."),
]
