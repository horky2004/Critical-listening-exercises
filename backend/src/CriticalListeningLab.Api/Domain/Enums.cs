namespace CriticalListeningLab.Api.Domain;

/// <summary>Rola korisnika. Cuva se u bazi, ne u Entra App Roles (odluka 13).</summary>
public enum UserRole
{
    Student = 0,
    Admin = 1
}

/// <summary>
/// Tip vjezbe. Odreduje koji generator pitanja se koristi i kako se
/// interpretira <c>ExerciseLevel.ConfigJson</c>.
/// Novi tip vjezbe = novi clan + novi generator, bez migracije sheme.
/// </summary>
public enum ExerciseType
{
    /// <summary>"Koja je frekvencija promijenjena?" - odgovor je frekvencija u Hz.</summary>
    EqFrequency = 0,

    /// <summary>"Koja frekvencija i u kojem smjeru?" - odgovor je npr. "2000:boost".</summary>
    EqFrequencyAndDirection = 1,

    /// <summary>Odabir iz konfiguriranih opcija - binarno, kolicina ili ratio.</summary>
    CompressionChoice = 2
}

public enum TestSessionStatus
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2
}

/// <summary>
/// Vrsta uvjeta za otkljucavanje levela. Zasad postoji samo jedna,
/// ali enum ostavlja prostor za druge uvjete bez mijenjanja sheme (odluka 6).
/// </summary>
public enum UnlockRequirementType
{
    PassedLevel = 0
}

/// <summary>
/// Stanje levela za jednog studenta na jednom audio izvoru.
/// Izvedeno, nikad se ne sprema u bazu (odluka 5).
/// </summary>
public enum LevelStatus
{
    /// <summary>Zahtjevi za otkljucavanje nisu zadovoljeni.</summary>
    Locked = 0,

    /// <summary>Dostupan, ali nijedan test jos nije zavrsen.</summary>
    Unlocked = 1,

    /// <summary>Barem jedan test zavrsen, ali level nikad nije prosao.</summary>
    InProgress = 2,

    /// <summary>Prosao barem jednom.</summary>
    Completed = 3
}
