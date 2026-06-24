# Evaluacija upotrebljivosti softvera DER Monitor

Samostalna studija iz predmeta Interakcija čovjek–računar

---

## Predmet studije

"DER Monitor" je WPF desktop aplikacija namijenjena praćenju distribuiranih energetskih resursa (solarni paneli i vjetrogeneratori). Aplikacija omogućuje korisniku postavljanje entiteta na mrežu prevlačenjem, uspostavljanje veza između entiteta, praćenje mjerenja u realnom vremenu te upravljanje listom entiteta. Interfejs je projektovan u dark temi prilagođenoj ekranima u industrijskom okruženju, mobilnih dimenzija (390×844px).

Evaluacija je izvršena prema Shneidermanovim zlatnim pravilima za dizajn interfejsa.

---

## Evaluacija po heuristikama

### 1. Težiti konzistentnosti

Aplikacija koristi jedinstven vizuelni identitet kroz sve poglede — tamna pozadina (`#1C2833`), bijeli tekst (`#ECF0F1`) i isti raspored elemenata konzistentni su na svakom ekranu. Svi entity kartoni na mreži imaju isti izgled: ikona tipa, naziv i trenutna vrijednost mjerenja. Navigacijska traka u dnu uvijek zauzima isti prostor (56px) i prikazuje ista četiri dugmeta bez obzira na aktivan pogled. Jedini uočeni propust je dualnost unosa teksta — na pogledu za dodavanje entiteta korisnik koristi standardni TextBox, dok je na pogledu za prikaz mreže prisutna virtualna tastatura kao overlay, što može zbuniti korisnika koji očekuje isti mehanizam unosa na oba mjesta.

---

### 2. Omogućiti frekventnijim korisnicima upotrebu prečica

Drag-and-drop je napredna prečica namijenjena iskusnom korisniku. Umjesto prolaska kroz meni za odabir pozicije, korisnik direktno prevlači entitet iz TreeViewa na željeni slot u gridu. Dodatna prečica za naprednog korisnika je swap entiteta — prevlačenjem entiteta sa jednog slota na drugi dolazi do direktne zamjene pozicija bez potrebe za uklanjanjem i ponovnim postavljanjem. Prevlačenje entiteta nazad u oblast TreeViewa uklanja entitet sa mreže bez otvaranja dijaloga. Aplikacija ne nudi tipkovne prečice (npr. Ctrl+Z za undo), što je propust za korisnike koji preferiraju rad sa tastaturom.

---

### 3. Davati informativni feedback

Sistem pruža kontinuiran vizuelni feedback. Zelene linije veze između entiteta iscrtavaju se odmah po uspostavljanju konekcije i brišu se automatski pri uklanjanju entiteta, što korisniku u svakom trenutku daje jasnu sliku topologije mreže. Vrijednosti mjerenja osvježavaju se svake sekunde putem tajmera, što korisnik direktno vidi na kartonima entiteta i na grafikonu mjerenja. Zona obavještavanja (NotificationArea) prikazuje sistemske poruke iznad svakog pogleda. Uočeni propust: pri restartu mjernog simulatora postoji pauza od ~1.5 sekundi tokom koje korisnik ne dobija nikakvu povratnu informaciju da se akcija izvršava.

---

### 4. Projektovati dijaloge naglašene zatvorenosti

Svaka sekvenca akcija u aplikaciji ima jasno definisan kraj. Dijalog za dodavanje entiteta nudi dugmad Sačuvaj i Odustani, čime korisnik uvijek ima eksplicitan izlaz. Dijalog za potvrdu brisanja je modalni prozor koji blokira ostatak aplikacije dok korisnik ne donese odluku — dugme Obriši vraća potvrdu akcije, dugme Odustani je poništava. Navigacija između četiri pogleda je trenutna i jednoznačna — korisnik uvijek zna na kom pogledu se nalazi i može se prebaciti jednim klikom.

---

### 5. Ponuditi prevenciju i rukovanje greškom

Aplikacija aktivno sprječava grešku umjesto da je samo prijavljuje. Mehanizam za interakciju sa slotom odbija drag na već zauzeti slot — korisnik ne može prepisati postavljeni entitet slučajnim prevlačenjem. Mehanizam za drop iz TreeViewa ne prihvata entitet koji je već prisutan na mreži, čime se onemogućava postavljanje duplikata. Veza između entiteta radi kao toggle — ponovni klik na isti par entiteta prekida vezu, bez mogućnosti dvostrukog uspostavljanja iste veze. Brisanje entiteta zahtijeva eksplicitnu potvrdu kroz modalni dijalog, što sprječava nenamjerno uklanjanje.

---

### 6. Dozvoliti poništavanje efekata akcije (Undo)

Aplikacija implementira puni undo/redo mehanizam putem steka u centralnom ViewModelu. Korisnik može poništiti postavljanje entiteta, premještanje, uspostavljanje i prekidanje veza te brisanje — bez ograničenja na samo posljednju akciju. Pored toga, drag entiteta nazad u oblast TreeViewa je intuitivni fizički undo koji ne zahtijeva traženje dugmeta za poništavanje. Kombinacija programskog undo steka i drag-based fizičkog poništavanja čini ovo pravilo jednim od najjače implementiranih u aplikaciji.

---

### 7. Interno podržavati kontrolu

Korisnik u svakom trenutku ima punu kontrolu nad sistemom. Sam odlučuje raspored entiteta na 3×4 gridu, sam bira koje entitete da poveže, sam pokreće i zaustavlja mjerni simulator. Destruktivne akcije (brisanje entiteta) uvijek prolaze kroz korak potvrde, čime se korisnik ne nalazi u situaciji da sistem uradi nešto bez njegove eksplicitne saglasnosti. Uočeni propust: restart simulatora automatski ubija i ponovo pokreće proces bez davanja korisniku opcije da to poništi ili zaustavi na pola puta.

---

### 8. Redukovati opterećenje radne memorije

TreeView sa grupiranim entitetima (po tipu: Solarni panel / Vetrogenerator) uvijek je vidljiv pored grida na pogledu za prikaz mreže — korisnik ne mora pamtiti koji entiteti postoje, već ih stalno vidi. Zelene linije veze vizualizuju topologiju mreže u realnom vremenu, oslobađajući korisnika od pamćenja koje stanice su spojene. Grafikon mjerenja prikazuje posljednjih 5 vrijednosti historije, čime korisnik može pratiti trend bez bilježenja. Navigacijska dugmad su uvijek vidljiva i jasno označena, pa korisnik ne mora pamtiti put do željenog pogleda.

---

## Zaključak

"DER Monitor" u velikoj mjeri zadovoljava Shneidermanova zlatna pravila. Najjače implementirana pravila su dozvoliti poništavanje efekata akcije (puni undo/redo stek) i ponuditi prevenciju greške (drag mehanizmi blokiraju neispravne akcije). Konzistentan vizuelni identitet kroz cijelu aplikaciju i stalni vizuelni feedback (linije veza, osvježavanje mjerenja) doprinose kvalitetu korisničkog iskustva.

Uočeni propusti — nedostatak tipkovnih prečica, dualnost mehanizma unosa teksta i nedostatak indikatora napretka pri restartu simulatora — predstavljaju oblasti za unapređenje, ali ne narušavaju osnovu upotrebljivosti aplikacije.
