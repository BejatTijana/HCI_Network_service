# NetworkService — DER Monitor

WPF MVVM desktop aplikacija za praćenje mjerenja solarnih panela i vjetroagregata u realnom vremenu putem TCP veze. Emulira mobilni interfejs (390×844px, portrait mode, miš kao jedini ulaz). Predmet: PSI IUIS, PZ2, FTN.

---

## Struktura foldera

```
NetworkService/
├── Commands/
│   └── RelayCommand.cs
├── Model/
│   ├── EntityType.cs
│   └── NetworkEntity.cs
├── ViewModel/
│   ├── MainWindowViewModel.cs
│   ├── HomeViewModel.cs
│   ├── NetworkEntitiesViewModel.cs
│   ├── AddEntityViewModel.cs
│   ├── NetworkDisplayViewModel.cs
│   ├── MeasurementGraphViewModel.cs
│   ├── VirtualKeyboardViewModel.cs
│   └── NetworkDisplayDragHandlers.cs
├── Views/
│   ├── HomeView.xaml / .xaml.cs
│   ├── NetworkEntitiesView.xaml / .xaml.cs
│   ├── AddEntityView.xaml / .xaml.cs
│   ├── NetworkDisplayView.xaml / .xaml.cs
│   ├── MeasurementGraphView.xaml / .xaml.cs
│   ├── VirtualKeyboardControl.xaml / .xaml.cs
│   └── ConfirmDeleteDialog.xaml / .xaml.cs
├── Resources/
│   └── Images/
│       ├── Logo.png
│       ├── SolarniPanel.png
│       └── Vetrogenerator.png
├── MainWindow.xaml / .xaml.cs
├── App.xaml / .xaml.cs
└── NetworkService.csproj
```

---

## Commands/

### `RelayCommand.cs`

Implementacija `ICommand` interfejsa koja se koristi za sve komande u aplikaciji.

**Zašto:** WPF MVVM zahtijeva da svaka akcija dugmeta bude vezana za komandu, a ne za event handler u code-behind fajlu. `RelayCommand` omogućuje kreiranje komandi direktno u ViewModelu kao lambda funkcije.

**Klasa `RelayCommand : ICommand`**

- `RelayCommand(Action<object> execute, Predicate<object> canExecute = null)` — konstruktor prima funkciju koja se izvršava (`execute`) i opcionalni predikat koji određuje da li je komanda trenutno dostupna (`canExecute`). Ako `execute` nije proslijeđen, baca `ArgumentNullException`.
- `CanExecute(object parameter)` — vraća `true` ako `canExecute` nije definisan ili ako je predikat ispunjen. WPF automatski poziva ovu metodu i siva dugme kada vrati `false`.
- `Execute(object parameter)` — poziva `execute` lambdu sa proslijeđenim parametrom.
- `CanExecuteChanged` — event koji se registruje na `CommandManager.RequerySuggested`. WPF automatski ponovo provjerava `CanExecute` pri svakoj promjeni u UI-u (kliku, unosu teksta itd.), što znači da dugmad automatski dobijaju/gube `IsEnabled` status bez ručnog notificiranja.

---

## Model/

### `EntityType.cs`

Definicija tipa mrežnog entiteta (DER — Distributed Energy Resource).

**Klasa `EntityType`**

- `string Ime` — naziv tipa koji se prikazuje u UI-u (npr. "Solarni panel").
- `string Slika` — putanja do slike tipa u obliku WPF pack URI-a (npr. `/Resources/Images/SolarniPanel.png`).
- `static readonly EntityType SolarniPanel` — unaprijed kreiran singleton za tip "Solarni panel". Koristi se svuda u aplikaciji umjesto kreiranja novih instanci.
- `static readonly EntityType Vetrogenerator` — unaprijed kreiran singleton za tip "Vetrogenerator".

**Zašto statični singletonovi:** Aplikacija ima tačno dva tipa entiteta koja su zadana specifikacijom (T4 kombinacija). Koristeći `static readonly`, garantuje se da se isti objekat koristi svuda — u modelu, UI-u, i `ComboBox`-u — pa poređenje referenci funkcioniše ispravno.

---

### `NetworkEntity.cs`

Model jednog mrežnog entiteta. Implementira `INotifyPropertyChanged` da bi WPF binding automatski ažurirao UI kad se vrijednosti promijene.

**Klasa `NetworkEntity : INotifyPropertyChanged`**

- `int ID` — jedinstveni identifikator entiteta. Auto-inkrement: `AddEntityViewModel` računa `Max(ID) + 1`.
- `string Naziv` — ime entiteta koje unosi korisnik. Validira se u `AddEntityViewModel`.
- `EntityType Tip` — tip entiteta (SolarniPanel ili Vetrogenerator). Sadrži ime i putanju do slike.
- `double LastValue` — poslednje primljeno mjerenje u MW od MeteringSimulatora. Opseg 0–10 MW; valjano je 1–5 MW.
- `bool LastValueValid` — `true` ako je `LastValue` u opsegu 1–5 MW. Postavljeno u `MainWindowViewModel.createListener()` svaki put kada stigne novo mjerenje.
- `List<MeasurementPoint> MeasurementHistory` — lista posjednjih 5 mjerenja. Inicijalizovana kao prazna lista; puni se u TCP listeneru. Ograničena na 5 elemenata: kad padne šesto mjerenje, prvo se uklanja.

**Ugniježđena klasa `MeasurementPoint`**

- `double Value` — vrijednost mjerenja.
- `DateTime Timestamp` — trenutak kad je mjerenje primljeno.
- `bool IsValid` — da li je vrijednost u valjanom opsegu (1–5 MW). Koristi se u grafikonu za boju kružića.

**`OnPropertyChanged(string propertyName)`** — poziva `PropertyChanged` event koji WPF sluša. Svaki setter poziva ovu metodu da obavijesti UI o promjeni vrijednosti.

---

## ViewModel/

Svi ViewModeli implementiraju `INotifyPropertyChanged`. Ni jedan ViewModel ne poziva UI direktno — sve promjene idu kroz property binding i komande.

### `MainWindowViewModel.cs`

Centralni ViewModel cijele aplikacije. Upravlja navigacijom između pogleda, TCP komunikacijom sa MeteringSimulatorom, undo stekom i pokretanjem/zaustavljanjem simulatora.

**Polja**

- `object _currentViewModel` — trenutno aktivan ViewModel; `ContentControl` u `MainWindow.xaml` prikazuje odgovarajući View putem `DataTemplate` mapiranja.
- `Stack<Action> _undoStack` — stek lambda funkcija koje poništavaju posljednju radnju. Svaki dodaj/brisanje gura lambdu na stek.
- `string _viewTitle` — naslov koji se prikazuje u title baru prozora.
- `bool _showBackButton` — kontroliše vidljivost dugmeta "‹" u title baru.
- `NotificationManager _notificationManager` — instanca Notification.Wpf biblioteke za toast notifikacije.
- `string SimulatorPath` — apsolutna putanja do `MeteringSimulator.exe`. Hardkodirana jer je lokacija fiksna na razvojnoj mašini.

**Properties**

- `ObservableCollection<NetworkEntity> Entities` — glavna kolekcija svih entiteta. Dijeli se sa svim ViewModelima (proslijeđena po referenci u konstruktoru). `ObservableCollection` automatski notificira UI kada se dodaju ili brišu elementi.
- `object CurrentViewModel` — kada se setuje, trigeriše `OnPropertyChanged` i za sebe i za `StatusBarBackground` jer boja status bara zavisi od toga koji je View aktivan.
- `string StatusBarBackground` — computed property: vraća `#1E8449` (zelena) kada je aktivan `NetworkEntitiesViewModel`, inače `#196F3D` (tamnija zelena). Nema zaseban backing field — uvijek se izračunava iz `CurrentViewModel`.
- `string ViewTitle` / `bool ShowBackButton` — prate se odvojeno jer se postavljaju na različitim mjestima u navigacionim komandama.

**Komande**

- `NavigateHomeCommand` — navigira na `HomeViewModel`, resetuje naslov na "DER MONITOR" i skriva back dugme.
- `NavigateToEntitiesCommand` — aktivira `NetworkEntitiesViewModel`, postavlja naslov "Network Entities".
- `NavigateToDisplayCommand` — aktivira `NetworkDisplayViewModel`, postavlja naslov "Network Display".
- `NavigateToGraphCommand` — aktivira `MeasurementGraphViewModel`, postavlja naslov "Graph view".
- `NavigateBackCommand` — pametna navigacija unazad: vraća na prethodni view koji je zapamćen u `_navBackVM`/`_navBackTitle`/`_navBackShowBack` trojci. Ako prethodni view nije zapamćen, ide na Home. Na taj način dupli klik na entitet u NetworkEntities → Graf → Back vas vraća na NetworkEntities, a ne na Home.
- `UndoCommand` — uzima vrh undo steka i poziva ga. `canExecute` predikat provjerava da li je stek prazan — WPF automatski sivi dugme kada nema šta poništiti.

**Konstruktor**

Kreira sve ViewModele u tačno određenom redoslijedu:
1. Prvo `_addEntityViewModel` — jer `_networkEntitiesViewModel` treba referencu na navigate-to-add callback.
2. Zatim `_networkEntitiesViewModel` — prima referencu na navigate callback koji aktivira `_addEntityViewModel`.
3. `ClearConnections` wiring: `_networkEntitiesViewModel.ClearConnections = id => _networkDisplayViewModel.ClearConnectionsForEntity(id)` — nakon što su oba VM kreirana, vezuju se tako da brisanje entiteta briše i njegove konekcije na canvasu.

Kreira tri unaprijed definisana entiteta (za odbranu):
- Solarni panel 1, vrijednost 3.2 MW (validna)
- Vetrogenerator 1, vrijednost 4.8 MW (validna)
- Solarni panel 2, vrijednost 0.5 MW (nije validna — prikazuje se crveno)

**`RestartMeteringSimulator(Action<string, string> showToast = null)`**

Statična metoda koja se poziva nakon dodavanja ili brisanja entiteta, jer simulator treba da zna novi broj entiteta. Prima opcionalni `showToast` callback za feedback korisniku.

Logika:
1. Odmah na UI niti prikazuje toast "Restartuje se..." — korisnik dobija feedback da se nešto dešava.
2. Pokreće se na `Task.Run()` (background thread) da ne blokira UI nit.
3. Pronalazi sve procese sa imenom "MeteringSimulator" i pokušava ih zaustaviti:
   - Prvo šalje `CloseMainWindow()` (čist izlaz),
   - Čeka maksimalno 2000ms sa `WaitForExit(2000)`,
   - Ako ne stane, poziva `Kill()` (prisilno gašenje).
4. Čeka 1500ms (`Thread.Sleep`) da OS oslobodi portove i handlje.
5. Pokreće simulator putem `cmd.exe /c start "" "<putanja>"` — direktno pokretanje exe-a ne radi zbog Windows UAC i path restrikcija.
6. Nakon uspješnog pokretanja, putem `Dispatcher.Invoke` prikazuje toast "Simulator je pokrenut" na UI niti.
7. Loguje svaki korak u `restart.log` u istom folderu kao simulator — korisno za dijagnostiku ako restart ne uspije.

**`createListener()`**

TCP listener koji prima mjerenja od MeteringSimulatora na portu 25675.

- Kreira `TcpListener` na svim interfejsima (`IPAddress.Any`), port 25675.
- Pokreće background thread koji u beskonačnoj petlji prima konekcije sa `tcp.AcceptTcpClient()`.
- Svaka konekcija se obrađuje u `ThreadPool.QueueUserWorkItem` — ne blokira listener thread.
- **"Need object count"**: šalje broj entiteta simulatoru koji tek počinje, da bi znao koliko entiteta ima.
- **"Entitet_N:V"**: parsira indeks `n` i vrijednost `v`, ažurira `Entities[n].LastValue` i `LastValueValid`, dodaje u `MeasurementHistory` (briše najstariji ako ih ima više od 5). Sve ažuriranje se radi na UI niti putem `Application.Current.Dispatcher.Invoke()` jer WPF ne dozvoljava modifikaciju binding kolekcija sa background threada. Zapisuje mjerenje u `log.txt` u radnom folderu aplikacije.

---

### `HomeViewModel.cs`

Prazan ViewModel za Home view. Postoji jer `MainWindow.xaml` koristi `DataTemplate` mapiranje `{x:Type vm:HomeViewModel}` → `HomeView`. Bez ViewModel klase ne bi bilo mapiranja.

---

### `NetworkEntitiesViewModel.cs`

Upravlja prikazom tabele entiteta, pretragom i brisanjem.

**Polja**

- `ObservableCollection<NetworkEntity> _source` — referenca na glavnu kolekciju iz `MainWindowViewModel`. Prati promjene (add/remove) putem `CollectionChanged` eventa.
- `Action _navigateToAdd` — callback za navigaciju na Add Entity ekran. Injektiran iz `MainWindowViewModel`.
- `Action<NetworkEntity> _navigateToGraph` — callback za navigaciju na Graf sa konkretnim entitetom. Prima `NetworkEntity` koji treba biti selektovan na grafu čim se otvori. Injektiran iz `MainWindowViewModel`.
- `Func<string, bool> _confirmDelete` — callback koji otvara `ConfirmDeleteDialog` i vraća `true`/`false`.
- `Action<string, string> _showToast` — callback za toast notifikacije.
- `Action _restartSimulator` — callback za restart MeteringSimulatora.
- `Action<Action> _pushUndo` — callback koji gura lambdu na undo stek.
- `Action<int> ClearConnections` — public property, defaultno no-op lambda `_ => {}`. Postavlja se iz `MainWindowViewModel` nakon što je `NetworkDisplayViewModel` kreiran. Poziva se pri brisanju entiteta da bi uklonio njegove konekcije sa canvasa.

**Properties**

- `ObservableCollection<NetworkEntity> FilteredEntities` — kolekcija koja se prikazuje u `ListView`. Uvijek se ponovo gradi iz `_source` sa `ApplyFilter()`.
- `string SearchText` — tekst iz search TextBox-a. Svaka promjena poziva `ApplyFilter()`.
- `bool SearchByName` / `bool SearchByType` — RadioButton bindingovi. Postavljanje jednog automatski postavlja drugi na `false` i poziva `ApplyFilter()`.
- `NetworkEntity SelectedEntity` — odabrani red u `ListView`. Komande `DeleteCommand` i `NavigateToGraphCommand` imaju `canExecute` koji provjeravaju da li je ovaj property null.

**Komande**

- `NavigateToGraphCommand` — poziva `_navigateToGraph(SelectedEntity)`. Navigira na Graf i odmah selektuje kliknuti entitet na grafikonu. `canExecute` vraća `false` dok ništa nije selektovano. Aktivira se duplim klikom na red u ListView-u putem `ListViewDoubleClickBehavior` attached propertija.

**Metode**

- `ApplyFilter()` — prazni `FilteredEntities` i ponovo je puni iz `_source` primjenjujući filter. Ako je `searchText` prazan, prikazuju se svi entiteti. Poređenje je case-insensitive (`ToLowerInvariant()`).
- `OnSourceChanged(...)` — event handler na `_source.CollectionChanged`. Kada se entitet doda ili obriše u glavnoj kolekciji (npr. undo operacija), filter se ponovo primjenjuje.

**`DeleteCommand`**

1. Provjerava da `SelectedEntity` nije null (`canExecute`).
2. Poziva `_confirmDelete` — otvara dijalog; ako korisnik otkaže, prekida.
3. Pamti referencu na obrisani entitet i njegov indeks u `_source`.
4. Briše entitet iz `_source`.
5. Poziva `ClearConnections(capturedEntity.ID)` — briše linije na canvasu.
6. Resetuje `SelectedEntity = null`.
7. Prikazuje toast notifikaciju.
8. Restartuje simulator.
9. Gura undo lambdu: vraća entitet na isti indeks pomoću `_source.Insert(Math.Min(capturedIndex, _source.Count), capturedEntity)`. `Math.Min` štiti od situacije gdje je indeks veći od trenutne dužine (npr. ako je drugi entitet obrisan u međuvremenu).

---

### `AddEntityViewModel.cs`

ViewModel za formu dodavanja novog entiteta.

**Properties**

- `int ID` — computed property (bez backing fielda): vraća `Max(existing IDs) + 1` ili 1 ako nema entiteta. Prikazuje se u disabled TextBox-u kao preview sledećeg ID-a.
- `string Naziv` — binding na TextBox za ime. Svaka promjena briše `NazivError`.
- `string NazivError` — poruka greške koja se prikazuje ispod TextBox-a kada validacija ne prođe.
- `bool ShowNazivError` — computed: `true` ako `NazivError` nije prazno. Koristi se sa `BooleanToVisibilityConverter` u XAML-u.
- `string TypError` — poruka greške ispod ComboBox-a kada tip nije odabran.
- `bool ShowTypError` — computed: `true` ako `TypError` nije prazno.
- `List<EntityType> Types` — fiksna lista `{ SolarniPanel, Vetrogenerator }` za ComboBox.
- `EntityType SelectedType` — odabrani tip u ComboBox-u. Default je `null` — korisnik mora eksplicitno odabrati tip. Svaka promjena briše `TypError`.

**`SaveCommand`**

1. Validira **oba polja istovremeno** koristeći `bool valid = true` pattern — ako `Naziv` prazan, postavlja `NazivError` i `valid = false`; ako `SelectedType == null`, postavlja `TypError` i `valid = false`. Ako `!valid`, prekida. Oba errora se prikazuju odjednom, a ne jedan po jedan.
2. Kreira novi `NetworkEntity` sa sledećim ID-em, unesenim imenom i odabranim tipom.
3. Dodaje entitet u `_entities` (shared kolekcija iz `MainWindowViewModel`).
4. Prikazuje toast notifikaciju.
5. Restartuje simulator.
6. Gura undo lambdu: uklanja novi entitet iz `_entities`.
7. Poziva `Reset()` i navigira nazad.

**`CancelCommand`** — poziva `Reset()` i navigira nazad bez čuvanja.

**`Reset()`** — vraća formu na početno stanje: `Naziv = ""`, `NazivError = ""`, `SelectedType = SolarniPanel`. Eksplicitno poziva `OnPropertyChanged(nameof(SelectedType))` i `OnPropertyChanged(nameof(ID))` jer se ovo ne postavlja kroz property setter.

---

### `NetworkDisplayViewModel.cs`

Najkompleksniji ViewModel. Upravlja 12 canvas slotova, drag-and-drop stanjem, konekcijama između entiteta i crtanjem linija.

**Ugniježđene klase**

**`CanvasSlot : INotifyPropertyChanged`**

Predstavlja jedan slot na canvasu (4×3 grid, ukupno 12 slotova).

- `NetworkEntity Entity` — entitet koji trenutno zauzima slot, ili `null` ako je prazan. Setter sub/unsub-uje `PropertyChanged` event entiteta (da bi slot znao kada mu se promijeni vrijednost mjerenja) i trigeriše 5 zavisnih propertyja.
- `bool IsEmpty` / `bool IsOccupied` — computed; kontrolišu vidljivost placeholder teksta i slike u DataTemplatu.
- `bool IsOutOfRange` — computed; `true` ako slot ima entitet sa `LastValueValid = false`. Koristi se u DataTriggeru za crveni tekst i isprekidani border.
- `string DisplayText` — computed; vraća "drag here" za prazne slotove, ili "Naziv\n1.2MW" za popunjene. Formatira vrijednost sa jednom decimalom i jedinicom MW.
- `bool IsSelectedForConnection` — `true` kada je ovaj slot odabran za kreiranje konekcije (čeka se drugi klik). Prikazuje se amber border (`#F39C12`).
- `OnEntityPropertyChanged(...)` — sub-uje se na `Entity.PropertyChanged`; kada entitet dobije novo mjerenje (TCP), slot notificira `IsOutOfRange` i `DisplayText`.

**`EntityGroup`**

- `string TypeName` — naziv tipa (npr. "Solarni panel").
- `List<NetworkEntity> Entities` — entiteti tog tipa koji nisu na canvasu.

**`ConnectionLineData`**

- `double X1, Y1, X2, Y2` — koordinate krajeva linije na canvasu. Izračunate iz indeksa slota.

**Polja**

- `List<(int, int)> _connections` — lista parova ID-eva koji su međusobno povezani linijom. Parovi su normalizovani: uvijek `(manji ID, veći ID)`, što onemogućuje duplikate bez obzira na redoslijed klikova.
- `int? _connectingEntityId` — ID entiteta koji je prvi kliknut u toku kreiranje/brisanja konekcije. `null` znači da nema aktivnog povezivanja.

**Properties**

- `ObservableCollection<CanvasSlot> CanvasSlots` — 12 slotova, inicijalizovano u konstruktoru sa `Enumerable.Range(0, 12)`.
- `List<EntityGroup> EntityGroups` — computed; grupira entitete koji NISU na canvasu po tipu, za prikaz u TreeView-u. Ponovo se izračunava svaki put kada se pozove `OnPropertyChanged(nameof(EntityGroups))`.
- `ObservableCollection<ConnectionLineData> ConnectionLines` — kolekcija podataka za crtanje linija. XAML `ItemsControl` bind-uje na nju i crta `Line` element za svaki unos.

**Metode**

- `PlaceEntity(CanvasSlot target, NetworkEntity entity)` — postavlja entitet u target slot. Ako entitet već stoji u drugom slotu, briše ga odatle (swap). Gura undo lambdu koja vraća staro stanje. Poziva `UpdateConnectionLines()` jer se koordinate linija mijenjaju.
- `RemoveFromSlot(CanvasSlot slot)` — briše entitet iz slota (vraća u TreeView). Briše sve konekcije tog entiteta. Gura undo lambdu.
- `GetEntityById(int id)` — traži entitet u globalnoj kolekciji po ID-u. Koristi se u drag-and-drop handlerima.
- `ConnectOrDisconnect(int entityId)` — upravljanje konekcijama dvoklikom:
  - Ako nema aktivnog odabira (`_connectingEntityId == null`): odabira entitet (highlightuje slot amber bojom) i vraća.
  - Ako je isti entitet kliknut drugi put: poništava odabir.
  - Ako su kliknuta dva različita entiteta: normalizuje par `(min, max)`, dodaje ili briše iz `_connections`, gura undo lambdu, boji slot nazad na neodabrano, ažurira linije.
- `ClearConnectionsForEntity(int entityId)` — briše sve konekcije koje uključuju zadati ID. Poziva se pri brisanju entiteta (iz `NetworkEntitiesViewModel`) i pri dragovanju entiteta nazad na TreeView.
- `SetConnectingEntityId(int? id)` — interna metoda: postavlja `_connectingEntityId` i ažurira `IsSelectedForConnection` na svim slotovima.
- `UpdateConnectionLines()` — rekonstruiše `ConnectionLines` kolekciju:
  - Prazni kolekciju.
  - Za svaki par u `_connections`: pronalazi indeks svakog entiteta u `CanvasSlots`.
  - Računa koordinate centra slota: `X = (index % 4) * 85.5 + 42.75`, `Y = (index / 4) * 74.0 + 37.0`. Dimenzije su kalibrovane na `UniformGrid` ćeliju od 76×70px + 2px margin = 80×74px efektivno, 4 kolone.
  - Ako jedan od dva entiteta nije na canvasu, linija se preskače.
- `OnEntitiesChanged(...)` — event handler na globalnoj kolekciji; briše iz slotova entitete koji više ne postoje (obrisani su), i ponovo izračunava `EntityGroups`.

---

### `MeasurementGraphViewModel.cs`

Upravlja grafikonom posjednjih 5 mjerenja za odabrani entitet.

**Properties**

- `ObservableCollection<NetworkEntity> Entities` — direktna referenca na globalnu kolekciju, za ComboBox.
- `NetworkEntity SelectedEntity` — entitet odabran u ComboBox-u. Setter sub/unsub-uje `PropertyChanged` eventa entiteta (da se grafikon osvježi kada stigne TCP mjerenje) i poziva `UpdateDisplayShapes()`.
- `List<NetworkEntity.MeasurementPoint> GraphPoints` — snapshot liste mjerenja odabranog entiteta u datom trenutku. `.ToList()` kreira kopiju da bi se spriječila race condition između TCP threada koji dodaje tačku i UI threada koji čita listu tokom crtanja.

**Ugniježđene klase**

- `DisplayEllipseData` — podaci za jedan kružić: `Left`, `Top` (Canvas.Left/Top pozicija), `ValueText` (formatirana vrijednost), `IsValid` (boja), `TimeLabel` (sat:minuta timestamp na X osi).
- `DisplayLineData` — podaci za jednu liniju između kružića: `X1, Y1, X2, Y2`.

**`ObservableCollection<DisplayEllipseData> DisplayEllipses`** i **`ObservableCollection<DisplayLineData> DisplayLines`** — XAML bind-uje na ove kolekcije sa tri zasebna `ItemsControl`-a na `Canvas`-u.

**`DispatcherTimer _timer`** — ticker koji se okida svaku sekundu na UI niti. Poziva `UpdateDisplayShapes()`. Razlog: grafikon se mora osvježavati čak i kada nema novih TCP mjerenja (da osvježi X-osi timestamps kad sati pređu u novi minut). `DispatcherTimer` se automatski izvršava na UI niti, pa nema potrebe za `Dispatcher.Invoke`.

**`UpdateDisplayShapes()`**

Rekonstruiše `DisplayEllipses` i `DisplayLines` iz trenutnih `GraphPoints`.

Algoritam raspoređivanja tačaka na canvasu (200×342px):
- X koordinata: ravnomjerno raspoređene tačke sa korakom 60px, počev od 60px. Pet tačaka zauzimaju od 60 do 300px.
- Y koordinata: normalizovana vrijednost u rasponu [20, 180]px. Minimum vrijednosti → vrh (Y=180), maksimum → dno (Y=20). Ako su sve vrijednosti iste, raspon se postavlja na 1.0 da bi se izbjeglo dijeljenje nulom.
- Kružić je 32×32px, centriran na (cx, cy) tako da je `Left = cx - 16`, `Top = cy - 16`.
- Linije spajaju susjedne tačke: za N tačaka ima N-1 linija.
- Timestamp se prikazuje ispod kružića na fiksnoj Y poziciji 185px.

---

### `VirtualKeyboardViewModel.cs`

Logika virtuelne tastature. Implementira `INotifyPropertyChanged`. Odvojena od XAML code-behind fajla prema MVVM principu (profesor zahtijeva: ni u jednom `.xaml.cs` ne smije biti ništa sem konstruktora).

**Polja**

- `SolidColorBrush ShiftActive` / `ShiftInactive` — statični brushevi za Shift dugme. `ShiftActive = #ECF0F1` (bijela), `ShiftInactive = #95A5A6` (siva). Statični su jer su isti za sve instance tastature i ne moraju se kreirati iznova.
- `bool _isShifted = true` — defaultno `true` jer je prirodno da se prvo slovo piše velikim slovom.

**Properties**

- `TextBox TargetTextBox` — referenca na TextBox u koji se upisuje tekst. Postavlja se izvana putem `VirtualKeyboardAttacher` attached property-ja, ne u konstruktoru. Nije binding jer je WPF kontrola, a ne ViewModel data.
- `bool IsShifted` — da li je Shift aktivan. Setter notificira i `ShiftForeground` jer boja dugmeta zavisi od ovog stanja.
- `SolidColorBrush ShiftForeground` — computed property koji vraća `ShiftActive` ili `ShiftInactive` na osnovu `_isShifted`.
- `ICommand KeyPressCommand` — sve tipke (uključujući Shift) koriste ovu istu komandu. Parametar komande je sadržaj dugmeta (`CommandParameter="{Binding Content, RelativeSource=...}"`), osim za Shift koji ima `CommandParameter="SHIFT"` (ASCII string da bi se izbjegla nepouzdana Unicode string poređenja za "⇧").

**`OnKeyPress(string key)`**

Centralna metoda za obradu unosa:
- `"⌫"` — briše karakter lijevo od kursora: `tb.Text.Remove(caret-1, 1)`, pomjera kursor.
- `"space"` — umeće razmak na poziciju kursora.
- `"↵"` — Enter tipka; trenutno bez akcije (Enter nije potreban za formu).
- `"SHIFT"` — toggle `IsShifted`.
- Slova (`char.IsLetter`): umeće slovo malo ili veliko ovisno o `IsShifted`, a potom resetuje `IsShifted = false` (auto-reset: shift važi samo za jedno slovo).
- Ostalo (cifre, znaci): umeće karakter direktno bez promjene Shift stanja.

Svo umetanje/brisanje radi direktno na `TextBox.Text` i `TextBox.CaretIndex`, što osigurava da kursor ostaje na ispravnoj poziciji čak i pri editovanju sredine teksta.

**`VirtualKeyboardAttacher` (statična klasa u istom fajlu)**

Attached property koji vezuje `TargetTextBox` na `VirtualKeyboardViewModel` instancu koja živi unutar `VirtualKeyboardControl`.

- `TargetTextBoxProperty` — `DependencyProperty` tipa `TextBox`, registrovana na tip `VirtualKeyboardAttacher`.
- `OnTargetTextBoxChanged(...)` — callback koji se poziva kada se attached property postavi u XAML-u:
  - Ako je element već učitan (`fe.IsLoaded`): odmah poziva `Apply()`.
  - Inače: subscribuje se na `Loaded` event i poziva `Apply()` kada se element učita.
  - `Apply()`: uzima `fe.DataContext as VirtualKeyboardViewModel` i postavlja `vm.TargetTextBox`.

Razlog za Loaded odgodu: `DataContext` elementa nije dostupan tokom XAML parsiranja, samo nakon što se element učita u vizuelno stablo.

---

### `NetworkDisplayDragHandlers.cs`

Sav Drag-and-Drop kôd za `NetworkDisplayView`. Odvojen od XAML code-behind fajla prema MVVM principu (profesor zahtijeva: ni u jednom `.xaml.cs` ne smije biti ništa sem konstruktora).

Sadrži tri behaviour klase i jednu attacher klasu.

**`TreeViewDragBehavior`**

Handles drag iz `TreeView`-a (entiteti koji nisu na canvasu) prema canvas slotovima.

- `Attach(TreeView tv)` — subscribuje se na `PreviewMouseLeftButtonDown` i `MouseMove`.
- `OnMouseDown(...)` — pamti početnu poziciju miša.
- `OnMouseMove(...)` — ako je levi taster pritisnut i miš je pomjeren više od Windows-ovog minimalnog drag praga (`SystemParameters.MinimumHorizontalDragDistance`), inicira drag putem `DragDrop.DoDragDrop(tv, entity.ID.ToString(), Move)`. Prenosi se ID entiteta kao string.

**`SlotInteractionBehavior`**

Handles drag sa canvas slota (zamjena mjesta entiteta), drop na slot i klik za konekciju.

- `Attach(FrameworkElement fe)` — subscribuje se na 5 eventova: `PreviewMouseLeftButtonDown`, `MouseMove`, `MouseLeftButtonUp`, `Drop`, `DragOver`.
- `OnMouseDown(...)` — pamti početnu poziciju, resetuje `_dragStarted = false`.
- `OnMouseMove(...)` — ako slot ima entitet i miš je pomjeren iznad praga, postavlja `_dragStarted = true` i inicira drag.
- `OnMouseUp(...)` — ako drag nije bio iniciran (`!_dragStarted`): poziva `ConnectOrDisconnect` na VM-u (klik = connect/disconnect). `e.Handled = true` sprečava propagaciju.
- `OnDrop(...)` — parsira ID iz drag podataka, pronalazi entitet putem VM-a, poziva `vm.PlaceEntity(slot, entity)`.
- `OnDragOver(...)` — postavlja `DragDropEffects.Move` ako postoje valjani podaci, inače `None`.
- `GetVM()` — traži `NetworkDisplayViewModel` hodajući gore po vizuelnom stablu od elementa slota (`VisualTreeHelper.GetParent`). Slot je `DataContext = CanvasSlot` koji ne poznaje VM direktno, pa se VM traži u roditeljskim elementima.

**`TVAreaDropBehavior`**

Handles drop na TreeView oblast (vraćanje entiteta sa canvasa u TreeView).

- `Attach(FrameworkElement fe)` — subscribuje se na `Drop` i `DragOver`.
- `OnDrop(...)` — parsira ID, pronalazi slot koji sadrži taj entitet, poziva `vm.RemoveFromSlot(slot)`.
- `OnDragOver(...)` — standardna logika dozvoljavanja dropa.

**`DragBehaviorAttacher` (statična klasa)**

Attached properties koji aktiviraju gore navedene behaviour klase direktno iz XAML-a, bez code-behind.

- `IsTVAreaDropProperty` — `bool`, kada se postavi na `True` na nekom elementu, kreira i attaches `TVAreaDropBehavior`.
- `IsTreeViewDragProperty` — `bool`, kada se postavi na `True` na `TreeView` elementu, kreira i attaches `TreeViewDragBehavior`.
- `IsSlotInteractionProperty` — `bool`, kada se postavi na `True` na slot `Grid` elementu, kreira i attaches `SlotInteractionBehavior`.

Razlog za ovaj pattern umjesto `Behavior<T>` iz `Microsoft.Xaml.Behaviors`: `Microsoft.Xaml.Behaviors` biblioteka je dio projekta (dodata kao zavisnost od `Notification.Wpf`), ali njeno korišćenje u XAML-u (`<b:Interaction.Behaviors>`) izaziva BAML kompajlerske greške u `_wpftmp.csproj` (MC3074, CS0234). Custom attached property pattern postiže isti efekat bez te zavisnosti.

**`ListViewDoubleClickBehavior` (statična klasa)**

Attached property koji omogućuje vezivanje komande na `MouseDoubleClick` event `ListView`-a iz čistog XAML-a.

- `CommandProperty` — `DependencyProperty` tipa `ICommand`. Kada se postavi na `ListView`, subscribuje se na `MouseDoubleClick` event.
- `OnCommandChanged(...)` — callback koji sub/unsub-uje event handler kada se property postavi ili promijeni.
- `OnDoubleClick(...)` — handler koji poziva komandu ako `CanExecute` vrati `true`.

**Zašto nije `InputBindings`/`MouseBinding`:** U WPF-u, `ListViewItem` konzumira mouse event prije nego što se propagira do `ListView`-a, pa `MouseBinding` na `ListView.InputBindings` nikad ne dobija double-click. Ovaj behavior se subscribuje direktno na `ListView.MouseDoubleClick` event koji se propagira ispravno.

---

## Views/

Svi `.xaml.cs` fajlovi sadrže samo konstruktor koji poziva `InitializeComponent()`. Nema logike u code-behind fajlovima.

### `MainWindow.xaml`

Glavni prozor aplikacije, dimenzija 390×844px (portrait mobile emulator), ne može se resajzovati.

**Struktura**

Sadrži `DataTemplate` mapiranja: kad `CurrentViewModel` postane određeni tip, WPF automatski prikazuje odgovarajući UserControl:
- `HomeViewModel` → `HomeView`
- `NetworkEntitiesViewModel` → `NetworkEntitiesView`
- `AddEntityViewModel` → `AddEntityView`
- `NetworkDisplayViewModel` → `NetworkDisplayView`
- `MeasurementGraphViewModel` → `MeasurementGraphView`

`DockPanel` sa tri zone:
- **Status bar** (vrh, 24px): prikazuje "9:41" i "●●●" da imitira mobilni status bar. Boja pozadine se mijenja putem `{Binding StatusBarBackground}` — zelena kada je aktivan Network Entities view, tamnija zelena inače.
- **Title bar** (ispod status bara, 48px): prikazuje `{Binding ViewTitle}`. Sadrži "‹" back dugme koje se prikazuje samo kada `ShowBackButton = true`.
- **Bottom nav bar** (dno, 56px): tri dugmadi: Home, Undo, Menu. Undo dugme automatski postaje disabled kada je undo stek prazan (zbog `RelayCommand.CanExecute`).
- **Content area** (ostatak): `ContentControl` bind-ovan na `CurrentViewModel`.

`NotificationArea` (Notification.Wpf): plutajući overlay u gornjem desnom uglu, `ZIndex=1000`, maksimalno 3 toasta odjednom.

**Boje (dark DER tema)**

- Pozadina: `#1C2833` (tamno plavo-siva)
- Kartice/inputi: `#2C3E50` (srednje tamno)
- Title/nav bar: `#1A3A2A` (tamno zelena)
- Tekst: `#ECF0F1` (bijela), `#95A5A6` (siva)
- Primarna akcija: `#1E8449` (zelena)
- Opasna akcija: `#C0392B` (crvena)
- Status bar (Entities view): `#1E8449`, (ostali viewovi): `#196F3D`

---

### `HomeView.xaml`

Početni ekran. Prikazuje logo, naziv aplikacije, podnaslov i tri navigaciona dugmeta (Network Entities, Network Display, Graph view). Navigacione komande se bind-uju na `DataContext` prozora putem `RelativeSource AncestorType=Window` jer `HomeView` nema vlastite komande.

---

### `NetworkEntitiesView.xaml`

Prikazuje tabelu svih entiteta sa pretragom, dugmadima za dodavanje/brisanje i virtuelnom tastaturom.

**Ključni dijelovi**

- **RadioButtons** (`SearchByName`, `SearchByType`) — dvije opcije pretrage.
- **TextBox** (`x:Name="searchTB"`) — polje pretrage bind-ovano na `SearchText`.
- **X dugme** — briše tekst pretrage (`ClearSearchCommand`).
- **ListView** (`ItemsSource="{Binding FilteredEntities}"`) — prikazuje entitete sa kolonama ID, Name, Type, MW. `AlternationCount=2` za naizmjenične boje redova. Sistemske boje selekcije prepisane su putem `Resources` da bi odgovarale tamnoj temi. `vm:ListViewDoubleClickBehavior.Command="{Binding NavigateToGraphCommand}"` omogućuje navigaciju na Graf duplim klikom na red.
- **Add dugme** (zeleno) — navigira na Add Entity formu.
- **Delete dugme** (crveno) — otvara dijalog za potvrdu; aktivan samo kada je odabran red.
- **VirtualKeyboardControl** — uvijek vidljiva ispod tabele; bind-ovana na `searchTB`.

---

### `AddEntityView.xaml`

Forma za dodavanje novog entiteta.

**Ključni dijelovi**

- **ID TextBox** — `IsEnabled="False"`, bind-ovan `Mode=OneWay` na `ID` property (read-only prikaz sledećeg ID-a). `Mode=OneWay` je neophodan jer bi `TwoWay` (defaultno) pokušao pisati u computed property i baciti grešku.
- **Name TextBox** (`x:Name="nameTB"`) — bind-ovan na `Naziv`. Placeholder tekst "Enter name..." prikazan je putem `DataTrigger` koji provjerava da li je `Naziv == ""`.
- **Error TextBlock** — prikazuje `NazivError` kada je `ShowNazivError = true`.
- **ComboBox** — custom `ControlTemplate` koji prikazuje `SelectedItem.Ime` direktno u `TextBlock`-u umjesto default `ContentPresenter`-a. Standardni WPF ComboBox ne prikazuje custom template u zatvorenom stanju na dark temi.
- **VirtualKeyboardControl** — bind-ovana na `nameTB`.
- **Cancel** / **Add** dugmad.

---

### `NetworkDisplayView.xaml`

Najkompleksniji View. Prikazuje TreeView entiteta koji nisu na canvasu i 4×3 grid canvas slotova sa konekcijskim linijama.

**TreeView oblast**

- `Border` sa `vm:DragBehaviorAttacher.IsTVAreaDrop="True"` — cijela oblast prima drop (vraćanje entiteta).
- `TreeView` sa `vm:DragBehaviorAttacher.IsTreeViewDrag="True"` — drag inicira prevlačenje entiteta.
- `HierarchicalDataTemplate`: prikazuje `EntityGroup.TypeName` kao root node i listu `Entities` kao child nodeove.
- `ItemContainerStyle IsExpanded="True"` — grupe su uvijek otvorene, ne kolapsuju se tokom drag operacije.

**Canvas area**

Dva `ItemsControl`-a su overlayed unutar `Grid`-a:

1. **Slotovi** (`ItemsSource="{Binding CanvasSlots}"`): `UniformGrid 3×4`. Svaki slot je `Grid 76×70` sa `vm:DragBehaviorAttacher.IsSlotInteraction="True"`. Unutar slota:
   - `Rectangle` sa `RadiusX=10` (zaobljeni ugaovi); isprekidan border kada je `IsOutOfRange=true`.
   - `Image` sa ikonom tipa entiteta; skriven kada je slot prazan (`Visibility={Binding IsOccupied}`).
   - `TextBlock` sa imenom i vrijednošću; crvene boje kada je `IsOutOfRange=true`.
   - Amber `Border` overlay (`IsHitTestVisible=False`) koji se prikazuje kada je `IsSelectedForConnection=true`.

2. **Linije** (`ItemsSource="{Binding ConnectionLines}"`): `Canvas` panel, `IsHitTestVisible=False` (ne prima klikove). Svaka linija je `Line` element bind-ovan na `ConnectionLineData` koordinate; boja `#2ECC71` (emerald zelena), debljina 2.5px, zaobljeni krajevi.

---

### `MeasurementGraphView.xaml`

Prikazuje grafikon posjednjih 5 mjerenja odabranog entiteta.

**ComboBox** — custom `ControlTemplate` sa `TextBlock` koji bind-uje `SelectedItem.Naziv`. Placeholder "— Izaberite entitet —" prikazan je kada je `SelectedEntity == null`.

**Graf area** (`Border 342×200`)

Tri `ItemsControl`-a na `Canvas`-u:
1. **Linije** (`DisplayLines`): sivi veznici između kružića.
2. **Kružići** (`DisplayEllipses`): 32×32px `Grid` sa `Ellipse` i `TextBlock`. Zeleni (`#1E8449`) za validna mjerenja, crveni sa isprekidanim bordi i shadow efektom (`#922B21`) za nevalidna. `Canvas.Left` i `Canvas.Top` postavljeni putem `ItemContainerStyle`.
3. **Timestamps** (`DisplayEllipses`): isti podaci, ali prikazuje `TimeLabel` ispod kružića na fiksnom `Canvas.Top=185`.

**Legenda** — statični `TextBlock`-ovi: "● Valid (1-5 MW)" zeleno, "○ Invalid" crveno.

---

### `VirtualKeyboardControl.xaml`

Virtuelna tastatura za unos teksta mišem. QWERTY raspored sa 5 redova.

**DataContext**: direktno u XAML-u: `<vm:VirtualKeyboardViewModel/>` — svaka instanca tastature kreira vlastiti ViewModel.

**Raspored tipki**

- Red 0: cifre 1–0 (10 tipki)
- Red 1: QWERTYUIOP (10 tipki)
- Red 2: ASDFGHJKL (9 tipki)
- Red 3: ⇧ ZXCVBNM ⌫ (9 tipki) — Shift ima `CommandParameter="SHIFT"`, ostale tipke `CommandParameter="{Binding Content}"`
- Red 4: space (rastegljivo) + ↵ (fiksno)

Sve tipke koriste isti `KeyButton` style i `Command="{Binding KeyPressCommand}"`. Shift tipka ima dodatno `Foreground="{Binding ShiftForeground}"` da vizualno indikuje stanje (bijela=aktivno, siva=neaktivno).

---

### `ConfirmDeleteDialog.xaml` / `.xaml.cs`

Modalni dijalog za potvrdu brisanja entiteta. Jedinstven slučaj gdje se logika nalazi u `.xaml.cs` jer je to `Window` (ne UserControl/ViewModel pattern), i logika se svodi na postavljanje `DialogResult`.

- `ConfirmDeleteDialog(string entityName)` — konstruktor postavlja poruku: "Delete \"Ime\"?\nYou can undo this action."
- `Delete_Click(...)` — postavlja `DialogResult = true`; pozivalac dobija `true` od `ShowDialog()`.
- `Cancel_Click(...)` — postavlja `DialogResult = false`.

Poziva se iz `MainWindowViewModel` konstruktora kao lambda:
```csharp
name => { var d = new ConfirmDeleteDialog(name); d.Owner = ...; return d.ShowDialog() == true; }
```
`d.Owner` se postavlja da bi dijalog bio centriran u odnosu na glavni prozor.

---

## Resources/Images/

- `Logo.png` — logo aplikacije, prikazuje se na Home ekranu (90×90px).
- `SolarniPanel.png` — ikona za tip "Solarni panel", prikazuje se u canvas slotovima i TreeView-u.
- `Vetrogenerator.png` — ikona za tip "Vetrogenerator".

Slike su kompajlirane kao `Resource` u `.csproj`-u i dostupne putem WPF pack URI-a (`/Resources/Images/naziv.png`).

---

## App.xaml / App.xaml.cs

`App.xaml` — entry point aplikacije. Definiše `StartupUri="MainWindow.xaml"`.

`App.xaml.cs` — standardni application lifecycle kod, bez custom logike.

---

## Ключne arhitekturne odluke

| Odluka | Razlog |
|---|---|
| Shared `ObservableCollection<NetworkEntity>` | Svi ViewModeli rade na istim podacima; entity update od TCP-a odmah se vidi svuda |
| Virtuelna tastatura uvijek vidljiva (ne pop-up) | Specifikacija CG3: mobilni emulator, miš kao jedini ulaz — tastatura mora biti na ekranu |
| `VirtualKeyboardAttacher` attached property | DataContext tastature je njen vlastiti VM; attacher ga dobavlja i postavlja `TargetTextBox` |
| Custom DnD attached properties | `Microsoft.Xaml.Behaviors` XAML integracija izaziva BAML greške u ovom projektu; custom pattern postiže isti efekat |
| `CommandParameter="SHIFT"` (ne "⇧") | Unicode string komparacija `key == "⇧"` nije pouzdana u .NET 4.7.2; ASCII string eliminiše problem |
| `CloseMainWindow()` + fallback `Kill()` | Direktan `Kill()` izazvao Windows crash dialog; čist izlaz je uvijek preferirani put |
| `DispatcherTimer` u grafikonu | Automatski se izvršava na UI niti; eliminiše potrebu za `Dispatcher.Invoke` u ticker logici |
| Koordinate linija u VM | Podaci o pozicijama su izvedeni iz indeksa i dimenzija slotova; VM ih izračunava, XAML samo crta |
