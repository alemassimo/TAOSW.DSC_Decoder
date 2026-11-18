# Piano di adattamento Linux — TAOSW DSC Decoder

## Problema e obiettivo

Rendere utilizzabile su Debian/Ubuntu la versione desktop del decoder DSC, mantenendo funzionanti l'applicazione Windows e il decoder Core. L'intervento aggiunge un'applicazione `TAOSW.DSC_Decoder.UI_linux` e un backend di acquisizione audio Linux; non si limita alla compilazione cross-platform, ma include packaging, test e verifica su Linux.

## Stato attuale rilevato

- I progetti Core, UI e Gateway usano .NET 8; il Core contiene la decodifica e riceve l'audio attraverso `IAudioCapture`.
- La UI usa Avalonia e in precedenza è stata pubblicata con successo per il RID `linux-x64`. La pubblicazione da Windows non verifica l'avvio né l'accesso all'audio su Linux.
- `Gateways/AudioCapture.cs` usa `NAudio.Wave.WaveInEvent` per enumerazione e acquisizione: questo è il principale blocco runtime per Linux. La console esistente usa lo stesso Gateway Windows.
- La UI crea direttamente `AudioCapture` e fa un cast a quella classe per collegare gli eventi di errore/stato: il contratto condiviso va completato per poter sostituire il backend senza duplicare la logica della finestra.
- La riproduzione dei WAV usa `System.Media.SoundPlayer`, che il compilatore segnala come API esclusivamente Windows.
- `FileLogger` salva per impostazione predefinita accanto all'eseguibile; in un'installazione di sistema Linux la directory potrebbe non essere scrivibile.
- Esistono test unitari del Core; non risultano test dedicati ai Gateway né una pipeline CI Linux.

## Approccio proposto

1. Conservare l'app Windows e il suo comportamento; aggiungere un eseguibile Linux separato.
2. Condividere viste, view-model e logica UI tra le due app senza mantenere copie indipendenti. Estrarre una libreria UI condivisa, oppure usare file collegati se la struttura Avalonia/XAML esistente consente di farlo in modo affidabile; scegliere la soluzione meno invasiva dopo una prova di build.
3. Estendere l'astrazione audio condivisa affinché la UI consumi un factory/servizio e gli eventi comuni, non tipi concreti Windows.
4. Aggiungere un Gateway Linux basato su PortAudio, con accesso ai dispositivi ALSA e ai dispositivi esposti da PulseAudio/PipeWire tramite l'host audio disponibile. La prima implementazione usa un wrapper P/Invoke minimale dell'API C di PortAudio (nessun binding NuGet) e richiede `libportaudio.so.2`, fornita su Debian/Ubuntu da `libportaudio2`; l'ABI va ancora validata su host Linux.
5. Mantenere anche su Linux le notifiche sonore usando un servizio di playback multipiattaforma; non lasciare chiamate a `SoundPlayer` nel codice UI condiviso.
6. Pubblicare e collaudare un primo target Debian/Ubuntu x64, documentando installazione, permessi, runtime e dipendenze native. In questa fase la console Windows esistente resta invariata; il target richiesto è l'app desktop Linux.

## Attività / todo

### T1 — Definire e rendere esplicita l'astrazione dei servizi audio
Esaminare tutti i consumatori di `IAudioCapture`; aggiungere al contratto gli eventi di stato/errore attualmente esposti solo dalla classe Windows e definire una factory/selezione del provider per ciascuna app. Evitare dipendenze UI da `AudioCapture` concreto. Mantenere la semantica dei dati letti dal decoder (PCM, formato, frequenza di campionamento e comportamento del buffer) documentata e verificabile.

### T2 — Verificare binding PortAudio e prova tecnica Linux
Selezionare un binding .NET adeguatamente mantenuto, verificare enumerazione dispositivi e acquisizione mono PCM alla frequenza richiesta, gestione del callback/buffer e rilascio risorse. Verificare ALSA e l'accesso ai dispositivi PulseAudio/PipeWire nelle versioni Debian/Ubuntu target; registrare le librerie native richieste e gli errori attesi quando non disponibili.

### T3 — Implementare il Gateway Linux
Creare il progetto Gateway Linux che implementa il contratto condiviso: lista dispositivi, avvio/arresto, buffering, errori/stato, cancellazione/rilascio e lettura dei file WAV per il test offline. Gestire in modo esplicito assenza di dispositivi, formati non supportati, permessi e indisponibilità della libreria nativa; preservare il formato PCM richiesto dal decoder.

### T4 — Introdurre la UI Linux mantenendo il comportamento Windows
Aggiungere `TAOSW.DSC_Decoder.UI_linux` con bootstrap Linux e riferimenti al Core/Gateway Linux. Condividere viste e logica UI con la versione Windows (preferibilmente tramite libreria condivisa o, se dimostrato più semplice e stabile, file collegati); mantenere XAML e risorse Avalonia funzionanti in entrambi i target. Eliminare dal progetto Linux manifest e impostazioni Windows-specifiche e usare il corretto tipo di output per Linux.

### T5 — Portare notifiche e percorsi dati
Introdurre un'astrazione di riproduzione WAV; conservare il player Windows e implementare un player compatibile Linux, oppure usare un player comune già supportato dal backend scelto. Spostare i log della UI in una directory utente scrivibile e conforme alle convenzioni Linux (XDG quando applicabile); verificare il caricamento delle risorse e i percorsi quando l'app è pubblicata fuori dalla directory di build.

### T6 — Test automatici e compatibilità
Aggiungere test del Gateway per enumerazione simulata/factory, validazione del formato e frequenza, lettura/buffer, errori, stop e dispose; usare fake o backend iniettabili per non dipendere da hardware nel test automatico. Eseguire i test Core esistenti e verificare che la build/publish Windows resti invariata. Aggiungere una build/test su runner Linux (o istruzioni riproducibili equivalenti) per entrambi i progetti condivisi e per la UI Linux.

### T7 — Pubblicazione, documentazione e collaudo su host Linux
Documentare prerequisiti Debian/Ubuntu, installazione di .NET 8 e librerie native audio, avvio, permessi audio, selezione del dispositivo, problemi comuni e differenze rispetto alla distribuzione Windows. Generare un artefatto `linux-x64` framework-dependent come modalità iniziale e verificare l'avvio su Debian/Ubuntu desktop reale con audio da file e dispositivo audio fisico/virtuale. Verificare decoding, arresto pulito, notifiche, export CSV e scrittura log. Il runtime .NET può essere incluso in una pubblicazione self-contained secondo T9, mentre PortAudio e i tool di riproduzione restano dipendenze native.

### T8 — Aggiungere una verifica locale Linux in Docker
Aggiungere un Dockerfile/test script riproducibile con SDK .NET 8 e `libportaudio2`; nel container Linux eseguire i test Core e Gateway, build e publish UI Linux e uno smoke test che confermi il caricamento/invocazione dell'API PortAudio senza richiedere dispositivi. Mantenere il container headless e non simulare come superata la cattura audio reale: il routing ALSA/PulseAudio/PipeWire e l'interfaccia grafica richiedono un test separato su host Linux o un runner configurato con i dispositivi necessari. Documentare avvio e limiti del test Docker.

### T9 — Creare un pacchetto Debian/Ubuntu self-contained
Produrre una pubblicazione `linux-x64` self-contained (`--self-contained true`, con runtime .NET incluso) e assemblarla in un pacchetto `.deb` installabile su Debian/Ubuntu x64. Dichiarare `libportaudio2` come dipendenza Debian obbligatoria e i player di notifica (`pulseaudio-utils`/`alsa-utils`) come facoltativi o raccomandati; il pacchetto deve fornire comando di avvio e voce desktop se le risorse UI necessarie sono disponibili. Aggiornare README e CI per generare e verificare il pacchetto; non assumere che self-contained includa librerie native di sistema. Non includere nel primo pacchetto `.deb` runtime o architetture oltre Linux x64; valutare un archivio `.tar.gz` solo se richiesto in seguito.

## Dipendenze tra attività

- T1 precede T3 e T4.
- T2 precede T3.
- T3 e T4 dipendono da T1; T4 dipende anche dalla prova di condivisione UI.
- T5 dipende dalla definizione della composizione UI/app e dalla scelta del backend audio.
- T6 verifica T1–T5 e accompagna T7.
- T8 dipende da T3, T4 e T6; integra ma non sostituisce T7.
- T9 dipende da T4, T6 e T8: il container Linux verifica la pubblicazione self-contained e l'assemblaggio `.deb`; T7 resta il collaudo hardware/audio reale.
- T7 conclude dopo il superamento dei test e delle build e dopo il collaudo audio su host Linux.

## Criteri di completamento

- L'app Windows continua a compilare e a catturare audio con il Gateway attuale.
- La UI Linux si pubblica e si avvia su Debian/Ubuntu x64 senza API Windows-only.
- L'enumerazione e l'acquisizione da un dispositivo ALSA/PulseAudio/PipeWire sono verificate su host Linux; il flusso PCM prodotto è accettato dal decoder senza regressioni.
- Errori per assenza di dispositivo/libreria e permessi sono mostrati o registrati esplicitamente; stop e dispose non lasciano callback o risorse attive.
- Test automatici indipendenti da hardware e test Core passano; i passaggi di installazione e avvio sono riproducibili dalla documentazione.
- Una verifica Docker basata su immagine Linux riproduce test, build/publish e smoke test di PortAudio senza dichiarare verificata la cattura da hardware.
- Il `.deb` installa e avvia l'app su Debian/Ubuntu x64 senza .NET Runtime preinstallato; le dipendenze native dichiarate sono installate dal gestore pacchetti.

## Assunzioni e scelte ancora modificabili

- La richiesta riguarda la UI desktop Linux; la console esistente resta fuori dal primo rilascio Linux, ma il Core rimane riutilizzabile.
- Il progetto Linux sarà separato (`TAOSW.DSC_Decoder.UI_linux`) e condividerà il codice UI, non ne manterrà una copia divergente.
- PortAudio è la scelta iniziale per coprire i dispositivi ALSA e i server audio desktop Linux; l'implementazione usa P/Invoke diretto alla libreria nativa. ABI e comportamento restano da verificare su Linux reale.
- Il primo target è Debian/Ubuntu x64, con .NET 8 installato e dipendenze native documentate; ARM64 e pacchetto `.deb` non sono requisiti iniziali.
- Gli effetti sonori Linux usano `paplay` con fallback `aplay`; il player Windows mantiene `SoundPlayer` dietro un servizio condiviso.
- Per il collaudo Docker si assume un container Linux headless: valida ambiente Linux e dipendenze native, ma non include accesso a `/dev/snd`, socket PipeWire/PulseAudio o display GUI. Su Docker Desktop Windows il pass-through audio non è assunto disponibile.
- Per la distribuzione si assume come default un pacchetto `.deb` Debian/Ubuntu x64 self-contained, preferito al solo archivio; la UI include il runtime .NET 8, ma `libportaudio2` rimane dipendenza di sistema. L'opzione `.tar.gz` può essere aggiunta successivamente senza cambiare il target.

## Piano follow-up — errori audio Linux e popup dopo la chiusura

### Problema osservato

Nel log del 18 novembre 2026, alle 15:00:54, appaiono molti eventi `Audio Capture Error` senza testo, seguiti dall'eccezione Avalonia `Cannot show a window with a closed owner`. Il callback UI attuale accoda un dialogo per ogni evento senza controllare se la finestra è in chiusura. L'eccezione del dialogo è quindi un effetto secondario; la causa dell'evento audio non è ancora stabilita. Il Gateway Linux presente nel sorgente emette messaggi non vuoti per overflow e failure del callback, perciò l'evento vuoto potrebbe provenire da una build diversa o da un altro percorso runtime.

### Ambito e assunzioni

- Limitare il comportamento modificato alla UI Linux; preservare il comportamento Windows e non cambiare l'algoritmo di decodifica.
- Individuare prima l'origine dell'evento vuoto e la build effettivamente avviata; non nascondere gli errori audio come soluzione.
- Durante un errore persistente mostrare al massimo un avviso per incidente mentre la finestra è attiva, mantenendo nei log il dettaglio e la frequenza degli eventi. In chiusura, registrare gli errori ma non accodare UI.
- Se l'evento non contiene descrizione, renderlo esplicito come errore audio senza dettagli invece di presentare un popup vuoto.

### Todo

1. **T10 — Tracciare l'evento di acquisizione vuoto.** Verificare quale artifact e provider sono in esecuzione, seguire tutte le emissioni `OnError` e riprodurre/registrare il contenuto originale dell'evento prima della formattazione UI.
2. **T11 — Correggere gestione e origine dell'errore Linux.** Risolto T10, correggere la causa confermata; proteggere il callback dal ciclo di chiusura, evitare l'accodamento ripetuto di dialoghi e mantenere diagnostica utile. Applicare la modifica solo al comportamento Linux.
3. **T12 — Verificare regressioni e comportamento.** Aggiungere/verificare test mirati per errori non vuoti e vuoti, eventi ripetuti e finestra in chiusura; eseguire i test pertinenti e build Linux e Windows.

Dipendenze: T11 dipende da T10; T12 dipende da T11.

### Criteri di completamento

- Nessun tentativo di mostrare finestre con owner chiuso e nessuna cascata di popup durante la chiusura.
- Un errore reale di acquisizione resta diagnosticabile e visibile una sola volta per incidente; un messaggio vuoto non produce un popup vuoto.
- La causa dell'evento audio viene identificata e corretta oppure documentata con evidenza riproducibile, senza confondere il problema del dialogo con il problema dell'acquisizione.
- Test e build Linux/Windows completati senza regressioni osservate.

## Avanzamento dell'implementazione

- Completati: contratto/factory audio condivisi, gateway PortAudio e lettura WAV, UI Linux con sorgenti UI/XAML condivise, player di notifica, log in directory utente, README e workflow CI Linux.
- Verificati localmente su Windows: build dell'intera soluzione, publish framework-dependent `linux-x64`, 31 test Core e 6 test Gateway Linux.
- Da collaudare: esecuzione dell'app e dell'ABI PortAudio su Debian/Ubuntu con `libportaudio2`, dispositivi ALSA/PulseAudio/PipeWire e hardware audio. Docker Desktop è installato ma il motore Linux non è avviato; non è disponibile una distribuzione WSL Linux.
- La CI aggiunta eseguirà test, build e publish su `ubuntu-latest` quando il workflow verrà attivato dal repository; non è stata possibile verificarne l'esecuzione in questa sessione.
- Nuovo intervento pianificato: test locale in container Linux headless con `libportaudio2`, per ridurre la differenza tra build Windows e runtime Linux. È complementare alla CI Ubuntu e al collaudo audio su host reale. Assunzione adottata perché l'utente non era disponibile a precisarla.
- Intervento pianificato su richiesta: `.deb` Debian/Ubuntu x64 self-contained, che incorpora il runtime .NET e dichiara esplicitamente le dipendenze native. Si assume il formato `.deb` come scelta predefinita perché il target è Debian/Ubuntu; l'utente può richiedere anche un archivio `.tar.gz`.
