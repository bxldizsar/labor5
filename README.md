---
tags: [MIP, csharp, winforms, entityframework, labor]
tantargy: Medii și instrumente de programare
tipus: laborator 5
---

# Laborator 5 – Bolt-kezelő Entity Frameworkkel

> [!summary] Röviden
> Egy WinForms app, ami egy **bolt termékeit** kezeli: listázás DataGridView-ban, keresés, új termék / mennyiség hozzáadása, eladás. Az adatbázist az **Entity Framework 6** kezeli (**Code First**, SQL Server **LocalDB**) – nincs kézzel írt SQL. Minden adatbázis-művelet **`Task.Run`**-nal a thread pool-on fut, és a végén **`Invoke`**-kal szól vissza a GUI szálnak.

Kapcsolódó: [[Proiect2 - Magazin EF SQLite]] (ennek a továbbfejlesztése) · [[Proiect1 - WebBrowser]]

---

## 1. Mi az az Entity Framework (EF)?

**ORM** = Object-Relational Mapper. C# osztályokat ír/olvas táblaként.

| C# | Adatbázis |
|---|---|
| `class Produs` | tábla |
| egy `Produs` objektum | egy sor |
| `int Id` property | elsődleges kulcs (automatikusan) |
| `DbContext` | maga az adatbázis-kapcsolat |
| `DbSet<Produs> Produse` | a tábla, amin LINQ-zni lehet |

A labor példája (`Person` + `PersonDbContext`) pontosan ez. Az EF az első futáskor **magától létrehozza** az adatbázist a LocalDB-ben (**Code First**).

Fontos műveletek:
```csharp
using (MagazinDbContext db = new MagazinDbContext())
{
    db.Produse.Add(p);        // INSERT
    db.Produse.Find(id);      // SELECT ... WHERE Id = id
    db.Produse.Remove(p);     // DELETE
    p.Cantitate += 5;         // UPDATE (az EF figyeli a változást)
    db.SaveChanges();         // itt megy ki minden az adatbázisba
}
```

---

## 2. Mi volt a feladat (cerințe)

| # | Követelmény | Magyarul |
|---|---|---|
| 1 | Proiect Windows Forms + model EF | Termék: **Id, Denumire (max 30), Descriere (max 500), DataIntrare, DataExpirare, Cantitate** |
| 2 | Form principal | **DataGridView** az összes termékkel, **Refresh** gomb, **MenuStrip**: Adăugare / Vânzare / Căutare |
| 3 | Căutare din Form-ul principal | Keresés = a DataGridView **szűrése** |
| 4 | Form distinct pentru adăugare | **Külön Form**. Két fajta: **új termék** és **mennyiség meglévőhöz** (két almenü). Meglévőnél az **Id-t a fő Formról** olvassa le a felhasználó |
| 5 | Form distinct pentru vânzare | **Dupla klikk** a soron nyitja; a **konstruktor** kapja meg a sor adatait; ha a mennyiség **0** lesz → **törlés** |
| 6 | Thread Pool + AsyncTask + invoke | Adatbázis-műveletek `Task.Run`-ban, a végén `Invoke` a GUI szálra az eredménnyel |

> [!tip] A kódban minden résznél ott van egy `// cerinta X - ...` komment, hogy melyik követelményt oldja meg.

---

## 3. Fájlok

```
Lab5_MagazinEF/
├─ ProiectMIP5.slnx
└─ ProiectMIP5/
   ├─ Produs.cs               ← a modell (cerința 1)
   ├─ MagazinDbContext.cs     ← DbContext + DbSet<Produs> Produse
   ├─ Form1.cs / .Designer    ← fő ablak: grid, keresés, menü (cerința 2, 3)
   ├─ FormAdaugare.cs         ← hozzáadás, 2 konstruktor (cerința 4)
   ├─ FormVanzare.cs          ← eladás (cerința 5)
   ├─ App.config              ← connection string a LocalDB-hez + EF beállítás
   └─ packages.config         ← EntityFramework 6.4.4
```

---

## 4. Hogyan működik – követelményenként

### Cerința 1 – a modell
`Produs.cs`:
```csharp
public int Id { get; set; }                  // EF: ez lesz a kulcs, magától számoz
[Required] [MaxLength(30)]  public string Denumire { get; set; }
[MaxLength(500)]            public string Descriere { get; set; }
public DateTime DataIntrare { get; set; }
public DateTime DataExpirare { get; set; }
public int Cantitate { get; set; }
```
A `[MaxLength]` miatt a táblában `nvarchar(30)` / `nvarchar(500)` lesz. A Form-on a TextBoxok `MaxLength`-je is 30 / 500, hogy be se lehessen írni többet.

### Cerința 2 – fő Form
- `dataGridView1` – csak olvasható, egész sor kijelölés.
- **Refresh** gomb → `IncarcaProduse("")`.
- Menü: `Adaugare produse` (▸ Produs nou, ▸ Cantitate la produs existent), `Vanzare produse`, `Cautare produse`.
- Extra: a **lejárt** termékek sora **piros**, alul státuszsor (hány termék van).

### Cerința 3 – keresés
A fő Form tetején: `Cauta produs: [______] [Cauta]`. A `Cautare produse` menü csak odaugrik a keresőmezőbe.
```csharp
if (filtru != "")
    query = query.Where(p => p.Denumire.Contains(filtru) || p.Descriere.Contains(filtru));
```
A szűrés **a névben és a leírásban** is keres. Üres mező + Refresh → újra minden látszik.

### Cerința 4 – hozzáadás
`FormAdaugare`-nek **két konstruktora** van:
- `FormAdaugare()` → **új termék**: minden mező kitölthető, az Id "(automat)".
- `FormAdaugare(int id)` → **meglévő termék**: csak az **Id** és a **mennyiség** írható. Az Id-t a fő Formon **kijelölt sorból** előre kitöltjük, de át lehet írni.

Mentéskor: meglévőnél `db.Produse.Find(id)` → ha nincs ilyen → hiba; ha van → `p.Cantitate += cantitate`.

### Cerința 5 – eladás
- `dataGridView1_CellDoubleClick` → a sorból kivesszük a `Produs` objektumot (`DataBoundItem`) → `new FormVanzare(p)`.
- A **konstruktor** kiírja az adatokat, és a `NumericUpDown` maximumát a készletre állítja (nem lehet többet eladni, mint amennyi van).
- Eladáskor **újra beolvassuk** a terméket az adatbázisból (hátha közben változott), levonjuk, és ha **0** lett → `db.Produse.Remove(p)`.

### Cerința 6 – Task.Run + Invoke
Ez a minta van mindenhol (betöltés, mentés, eladás):
```csharp
Task.Run(() =>                                   // 1. thread pool szál
{
    List<Produs> lista;
    using (MagazinDbContext db = new MagazinDbContext())
        lista = db.Produse.ToList();             // 2. lassú DB munka

    this.Invoke(new Action(() =>                 // 3. vissza a GUI szálra
        AfiseazaProduse(lista)));                //    az eredménnyel
});
```
> [!important] Miért kell az `Invoke`?
> A WinForms vezérlőket **csak az a szál** módosíthatja, amelyik létrehozta őket (GUI szál). Ha a `Task.Run`-ból közvetlenül írnánk a gridbe → `InvalidOperationException` (cross-thread operation). Az `Invoke` "átküldi" a kódot a GUI szálra.

> [!note] Miért lassú az első indítás?
> Az EF az első kapcsolatnál felépíti a modellt és létrehozza az adatbázist → pár másodperc. Pont ezért kell a Task.Run (a feladat is ezt írja: "vezi deschiderea inițială a conexiunii") – közben nem fagy le az ablak, a státuszsorban "Se incarca datele..." látszik.

---

## 5. Futtatás

1. `ProiectMIP5.slnx` megnyitása → Build (a NuGet magától letölti az EntityFramework 6.4.4-et).
2. **F5**. Az első indítás lassabb (létrejön az adatbázis).
3. Az adatok megnézése (mint a labor diáin): **View → SQL Server Object Explorer** → `(localdb)\MSSQLLocalDB` → `Databases` → `ProiectMIP5.MagazinDbContext` → `Tables` → jobb klikk a táblán → **View Data**.

> [!warning] Ha hibát dob a kapcsolódásnál
> Kell a **LocalDB** (Visual Studio Installer → "Data storage and processing" vagy "SQL Server Express LocalDB"). A labor diáin is ez van. A connection string az `App.config`-ban: `Data Source=(localdb)\MSSQLLocalDB`.

---

## 6. Kérdések, amikre jó, ha tudod a választ

> [!question]- Mi a `DbContext` és a `DbSet`?
> `DbContext` = az adatbázis (kapcsolat + változáskövetés). `DbSet<Produs>` = egy tábla, amin LINQ-val lehet lekérdezni.

> [!question]- Mikor kerül be ténylegesen az adat az adatbázisba?
> A `SaveChanges()`-nél. Addig csak a context "jegyzi" a változásokat.

> [!question]- Miért van `using` a context körül?
> Hogy a végén lezárja a kapcsolatot (`Dispose`), akkor is, ha kivétel jön.

> [!question]- Miért olvassuk újra a terméket eladáskor, miért nem a gridből vett objektumot használjuk?
> A gridben lévő adat lehet régi. Az adatbázisban lévő az igazi → ott ellenőrizzük a készletet.

> [!question]- Mi a `DataBoundItem`?
> A DataGridView sorához tartozó eredeti objektum (itt egy `Produs`), mert a grid `List<Produs>`-hoz van kötve.

> [!question]- Mi történik, ha 0 lesz a mennyiség?
> `db.Produse.Remove(p)` → a `SaveChanges()` kitörli a sort.
