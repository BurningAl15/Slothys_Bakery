using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Installs and populates French (fr), Italian (it), German (de), and Portuguese (pt)
    /// alongside Spanish (es) and English (en) across all Slothy's Bakery StringTableCollections.
    /// </summary>
    public static class BakerySixLanguageInstaller
    {
        private static readonly string[] RequiredCodes = { "es", "en", "fr", "it", "de", "pt" };
        private static readonly string[] Collections = { "BakeryShell", "GamePhrases", "Notifications", "UI" };

        private sealed class Row
        {
            public readonly string Fr, It, De, Pt;
            public Row(string fr, string it, string de, string pt) { Fr = fr; It = it; De = de; Pt = pt; }
            public string For(string code) => code switch
            {
                "fr" => Fr,
                "it" => It,
                "de" => De,
                "pt" => Pt,
                _ => null,
            };
        }

        private static readonly Dictionary<string, Row> Translations = new(StringComparer.Ordinal)
        {
            // ── BakeryShell ──────────────────────────────────────────────────────
            ["settings.title"] = new("Paramètres", "Impostazioni", "Einstellungen", "Definições"),
            ["settings.sound"] = new("Son", "Suono", "Ton", "Som"),
            ["settings.music"] = new("Musique", "Musica", "Musik", "Música"),
            ["settings.effects"] = new("Effets", "Effetti", "Effekte", "Efeitos"),
            ["settings.game"] = new("Jeu", "Gioco", "Spiel", "Jogo"),
            ["settings.vibration"] = new("Vibration", "Vibrazione", "Vibration", "Vibração"),
            ["settings.reminders"] = new("Rappel quotidien", "Promemoria giornaliero", "Tägliche Erinnerung", "Lembrete diário"),
            ["settings.language"] = new("Langue", "Lingua", "Sprache", "Idioma"),
            ["settings.change"] = new("Changer", "Cambia", "Ändern", "Alterar"),
            ["settings.more"] = new("Plus", "Altro", "Mehr", "Mais"),
            ["settings.achievements"] = new("Succès", "Obiettivi", "Erfolge", "Conquistas"),
            ["settings.rate"] = new("Évaluer", "Valutaci", "Bewerten", "Avaliar"),
            ["settings.share"] = new("Partager", "Condividi", "Teilen", "Partilhar"),
            ["settings.more_games"] = new("Plus de jeux", "Altri giochi", "Mehr Spiele", "Mais jogos"),
            ["settings.educator_feedback"] = new("Avis d'enseignants", "Feedback docenti", "Lehrer-Feedback", "Ideias de docentes"),

            ["language.title"] = new("Langue", "Lingua", "Sprache", "Idioma"),
            ["language.choose"] = new("Choisis ta langue", "Scegli la tua lingua", "Wähle deine Sprache", "Escolhe o teu idioma"),
            ["language.continue"] = new("Continuer", "Continua", "Weiter", "Continuar"),

            ["home.play"] = new("Jouer", "Gioca", "Spielen", "Jogar"),
            ["home.continue"] = new("Continuer", "Continua", "Fortsetzen", "Continuar"),
            ["home.settings"] = new("Paramètres", "Impostazioni", "Einstellungen", "Definições"),
            ["home.progress"] = new("Mes progrès", "I miei progressi", "Mein Fortschritt", "Meu progresso"),
            ["home.new_badge"] = new("Aujourd'hui !", "Oggi!", "Heute!", "Hoje!"),

            ["moregames.title"] = new("Plus de jeux", "Altri giochi", "Mehr Spiele", "Mais jogos"),
            ["moregames.subtitle"] = new("Plus de jeux de FatCat Game Studio", "Altri giochi di FatCat Game Studio", "Mehr Spiele von FatCat Game Studio", "Mais jogos do FatCat Game Studio"),
            ["moregames.empty"] = new("D'autres jeux arrivent bientôt.", "Altri giochi in arrivo presto.", "Weitere Spiele kommen bald.", "Mais jogos em breve."),
            ["moregames.visit_store"] = new("Voir dans la boutique", "Vedi nel negozio", "Im Store ansehen", "Ver na loja"),
            ["moregames.view"] = new("Voir", "Vedi", "Ansehen", "Ver"),

            ["gate.title"] = new("Demande à un adulte", "Chiedi a un adulto", "Frag einen Erwachsenen", "Pede ajuda a um adulto"),
            ["gate.question"] = new("Combien font {0} × {1} ?", "Quanto fa {0} × {1}?", "Wie viel ist {0} × {1}?", "Quanto é {0} × {1}?"),
            ["gate.hint"] = new("Ceci ouvre une page en dehors du jeu.", "Questo apre una pagina fuori dal gioco.", "Dies öffnet eine Seite außerhalb des Spiels.", "Isto abre uma página fora do jogo."),
            ["gate.lock"] = new("Réservé aux adultes", "Solo per adulti", "Nur für Erwachsene", "Apenas para adultos"),

            ["modes.button"] = new("Modes", "Modalità", "Modi", "Modos"),
            ["modes.title"] = new("Modes de jeu", "Modalità di gioco", "Spielmodi", "Modos de jogo"),
            ["modes.practice"] = new("Pratique par thème", "Pratica per tema", "Nach Thema üben", "Prática por tema"),
            ["modes.practice_short"] = new("Pratique", "Pratica", "Übung", "Prática"),
            ["modes.runs"] = new("Défis", "Sfide", "Herausforderungen", "Desafios"),
            ["modes.mg1"] = new("Comparer par étapes", "Confronto a passi", "Schrittweiser Vergleich", "Comparação por passos"),
            ["modes.mg2"] = new("Choisir le signe", "Scegli il segno", "Zeichen wählen", "Escolher o sinal"),
            ["modes.mg3"] = new("Écrire la fraction", "Scrivi la frazione", "Bruch schreiben", "Escrever a fração"),
            ["modes.daily"] = new("Défi du jour", "Sfida del giorno", "Tagesherausforderung", "Desafio diário"),
            ["modes.afternoon"] = new("Service de l'après-midi", "Turno pomeridiano", "Nachmittagsschicht", "Turno da tarde"),
            ["modes.chef"] = new("Mode Chef", "Modalità Chef", "Chef-Modus", "Modo Chef"),

            ["practice.subtitle"] = new("Choisis un thème pour t'entraîner à ton rythme", "Scegli un tema per esercitarti con calma", "Wähle ein Thema zum Üben in deinem Tempo", "Escolhe um tema para praticar sem pressa"),
            ["practice.mg1.desc"] = new("Compare les dénominateurs avant de choisir un signe", "Confronta i denominatori prima di scegliere il segno", "Vergleiche Nenner, bevor du ein Zeichen wählst", "Compara denominadores antes de escolher o sinal"),
            ["practice.mg2.desc"] = new("Choisis plus grand, plus petit ou égal entre deux plateaux", "Scegli maggiore, minore o uguale tra due vassoi", "Wähle größer, kleiner oder gleich zwischen zwei Tabletts", "Escolhe maior, menor ou igual entre duas bandejas"),
            ["practice.mg3.desc"] = new("Construis une fraction équivalente pour chaque recette", "Costruisci una frazione equivalente per ogni ricetta", "Bilde einen gleichwertigen Bruch für jedes Rezept", "Monta a fração equivalente de cada receita"),

            ["progress.title"] = new("Mes progrès", "I miei progressi", "Mein Fortschritt", "Meu progresso"),
            ["dash.t_story"] = new("Histoire", "Storia", "Geschichte", "História"),
            ["dash.t_afternoon"] = new("Service de l'après-midi", "Turno pomeridiano", "Nachmittagsschicht", "Turno da tarde"),
            ["dash.t_chef"] = new("Mode chef", "Modalità chef", "Chef-Modus", "Modo chef"),
            ["dash.t_daily"] = new("Défi du jour", "Sfida del giorno", "Tagesherausforderung", "Desafio diário"),
            ["dash.t_practice"] = new("Pratique", "Pratica", "Übung", "Prática"),
            ["dash.t_achievements"] = new("Succès", "Obiettivi", "Erfolge", "Conquistas"),
            ["dash.story_done"] = new("Terminée !", "Completata!", "Abgeschlossen!", "Concluída!"),
            ["dash.story_now"] = new("En cours : {0}", "Ora: {0}", "Jetzt: {0}", "Agora: {0}"),
            ["dash.times"] = new("Joué {0} fois", "Giocato {0} volte", "{0} Mal gespielt", "Jogado {0} vezes"),
            ["dash.open"] = new("Ouvert : essaie-le !", "Aperto: provalo!", "Offen: probier es!", "Aberto: experimenta!"),
            ["dash.locked_story"] = new("Termine l'histoire pour l'ouvrir", "Completa la storia per aprirlo", "Beende die Geschichte zum Öffnen", "Termina a história para abrir"),
            ["dash.locked_afternoon"] = new("Termine le service de l'après-midi pour l'ouvrir", "Completa il turno pomeridiano per aprirlo", "Beende die Nachmittagsschicht zum Öffnen", "Termina o turno da tarde para abrir"),
            ["dash.achievements"] = new("{0} sur {1} obtenus", "{0} di {1} ottenuti", "{0} von {1} erhalten", "{0} de {1} ganhos"),
            ["dash.see_achievements"] = new("Voir les succès", "Vedi obiettivi", "Erfolge ansehen", "Ver conquistas"),

            ["achievements.title"] = new("Succès", "Obiettivi", "Erfolge", "Conquistas"),
            ["achievement.progress"] = new("Succès débloqués", "Obiettivi sbloccati", "Erfolge freigeschaltet", "Conquistas desbloqueadas"),
            ["achievement.toast"] = new("Succès débloqué !", "Obiettivo sbloccato!", "Erfolg freigeschaltet!", "Conquista desbloqueada!"),
            ["achievement.unlocked"] = new("Débloqué", "Sbloccato", "Freigeschaltet", "Desbloqueado"),
            ["achievements.done"] = new("Obtenu !", "Ottenuto!", "Erhalten!", "Concluído!"),
            ["achievements.summary"] = new("{0} sur {1} succès débloqués", "{0} di {1} obiettivi sbloccati", "{0} von {1} Erfolgen freigeschaltet", "{0} de {1} conquistas desbloqueadas"),

            ["achievement.first.title"] = new("Première commande", "Primo ordine", "Erste Bestellung", "Primeiro pedido"),
            ["achievement.first.body"] = new("Termine ton premier mini-jeu.", "Completa il tuo primo minigioco.", "Schließe dein erstes Minispiel ab.", "Completa o teu primeiro minijogo."),
            ["achievement.orders.title"] = new("Boulangerie en marche", "Panetteria in moto", "Bäckerei in Schwung", "Padaria em movimento"),
            ["achievement.orders.body"] = new("Termine 10 mini-jeux.", "Completa 10 minigiochi.", "Schließe 10 Minispiele ab.", "Completa 10 minijogos."),
            ["achievement.perfect.title"] = new("Commande parfaite", "Ordine perfetto", "Perfekte Bestellung", "Pedido perfeito"),
            ["achievement.perfect.body"] = new("Termine un mini-jeu sans erreur.", "Completa un minigioco senza errori.", "Schließe ein Minispiel fehlerfrei ab.", "Completa um minijogo sem erros."),
            ["achievement.practice.title"] = new("Première pratique", "Prima pratica", "Erste Übung", "Primeira prática"),
            ["achievement.practice.body"] = new("Termine une pratique par thème.", "Completa una pratica per tema.", "Schließe eine Themenübung ab.", "Completa uma prática por tema."),
            ["achievement.practice3.title"] = new("Essaie chaque recette", "Prova ogni ricetta", "Probiere jedes Rezept", "Experimenta cada receita"),
            ["achievement.practice3.body"] = new("Pratique les trois mini-jeux.", "Esercitati in tutti e tre i minigiochi.", "Übe alle drei Minispiele.", "Pratica os três minijogos."),
            ["achievement.daily.title"] = new("Boulanger du jour", "Fornaio del giorno", "Tagesbäcker", "Padeiro diário"),
            ["achievement.daily.body"] = new("Termine un défi du jour.", "Completa una sfida del giorno.", "Schließe eine Tagesherausforderung ab.", "Completa um desafio diário."),
            ["achievement.daily5.title"] = new("Client fidèle", "Cliente abituale", "Stammgast", "Cliente habitual"),
            ["achievement.daily5.body"] = new("Termine cinq défis du jour.", "Completa cinque sfide del giorno.", "Schließe fünf Tagesherausforderungen ab.", "Completa cinco desafios diários."),
            ["achievement.shift.title"] = new("Service de l'après-midi", "Turno pomeridiano", "Nachmittagsschicht", "Turno da tarde"),
            ["achievement.shift.body"] = new("Termine un service de l'après-midi.", "Completa un turno pomeridiano.", "Schließe eine Nachmittagsschicht ab.", "Completa um turno da tarde."),
            ["achievement.chef.title"] = new("Choix du chef", "Scelta dello chef", "Wahl des Chefs", "Escolha do chef"),
            ["achievement.chef.body"] = new("Termine une partie en Mode Chef.", "Completa una partita in Modalità Chef.", "Schließe eine Runde im Chef-Modus ab.", "Completa uma partida no Modo Chef."),
            ["achievement.days.title"] = new("De retour à la boulangerie", "Di nuovo in panetteria", "Zurück in der Bäckerei", "De volta à padaria"),
            ["achievement.days.body"] = new("Joue pendant trois jours différents.", "Gioca in tre giorni diversi.", "Spiele an drei verschiedenen Tagen.", "Joga em três dias diferentes."),

            ["reminder.title"] = new("Slothy t'attend à la boulangerie", "Slothy ti aspetta in panetteria", "Slothy wartet in der Bäckerei auf dich", "O Slothy espera por ti na padaria"),
            ["reminder.daily"] = new("Une nouvelle activité de fractions t'attend. Tu essaies ?", "C'è una nuova attività sulle frazioni per te. Vuoi provare?", "Es gibt eine neue Bruch-Aktivität für dich. Lust mitzumachen?", "Há uma nova atividade de frações para ti. Queres tentar?"),
            ["reminder.miss"] = new("Tu manques à Slothy pour les commandes d'aujourd'hui.", "A Slothy manca il tuo aiuto con gli ordini di oggi.", "Slothy vermisst deine Hilfe bei den heutigen Bestellungen.", "O Slothy sente falta da tua ajuda nos pedidos de hoje."),
            ["reminder.comeback"] = new("La boulangerie est encore ouverte. Slothy t'a gardé quelque chose.", "La panetteria è ancora aperta. Slothy ti ha tenuto qualcosa.", "Die Bäckerei hat noch offen. Slothy hat dir etwas aufgehoben.", "A padaria ainda está aberta. O Slothy guardou algo para ti."),
            ["reminder.channel"] = new("Un petit rappel pour revenir jouer.", "Un gentile promemoria per tornare a giocare.", "Eine freundliche Erinnerung zum Weiterspielen.", "Um lembrete simpático para voltares a jogar."),

            ["friends.title"] = new("Amis de Slothy", "Amici di Slothy", "Slothys Freunde", "Amigos do Slothy"),
            ["friends.new"] = new("Nouveau !", "Nuovo!", "Neu!", "Novo!"),
            ["friends.see_all"] = new("Voir tous les jeux", "Vedi tutti i giochi", "Alle Spiele ansehen", "Ver todos os jogos"),

            ["diploma.open"] = new("Voir mon diplôme", "Vedi il mio diploma", "Mein Diplom ansehen", "Ver o meu diploma"),
            ["diploma.kicker"] = new("DIPLÔME DE", "DIPLOMA DI", "DIPLOM ALS", "DIPLOMA DE"),
            ["diploma.title"] = new("Assistant étoile", "Assistente stella", "Stern-Assistent", "Assistente estrela"),
            ["diploma.awarded"] = new("Décerné à", "Assegnato a", "Verliehen an", "Concedido a"),
            ["diploma.name_hint"] = new("Touche pour écrire ton nom", "Tocca e scrivi il tuo nome", "Tippe, um deinen Namen einzugeben", "Toca e escreve o teu nome"),
            ["diploma.store"] = new("Joue avec Slothy !", "Gioca con Slothy!", "Spiel mit Slothy!", "Joga com o Slothy!"),
            ["diploma.share"] = new("Partager", "Condividi", "Teilen", "Partilhar"),
            ["diploma.save"] = new("Enregistrer", "Salva", "Speichern", "Guardar"),
            ["diploma.close"] = new("Fermer", "Chiudi", "Schließen", "Fechar"),
            ["diploma.share_text"] = new("Mon diplôme de Slothy's Bakery !", "Il mio diploma di Slothy's Bakery!", "Mein Slothy's Bakery-Diplom!", "O meu diploma de Slothy's Bakery!"),

            ["final.title"] = new("Assistant étoile !", "Assistente stella!", "Stern-Assistent!", "Assistente estrela!"),
            ["final.subtitle"] = new("Tu as terminé l'histoire de Slothy's Bakery !", "Hai finito la storia di Slothy's Bakery!", "Du hast die Geschichte von Slothy's Bakery beendet!", "Terminaste a história de Slothy's Bakery!"),
            ["final.back"] = new("Retour à l'accueil", "Torna all'inizio", "Zurück zum Start", "Voltar ao início"),

            ["summary.ok"] = new("Continuer", "Continua", "Weiter", "Continuar"),
            ["feedback.streak"] = new("Série ×{0} !", "Serie ×{0}!", "Serie ×{0}!", "Série ×{0}!"),
            ["feedback.right"] = new("Bravo !", "Esatto!", "Richtig!", "Certo!"),
            ["feedback.try_again"] = new("Essaie encore", "Riprova", "Versuch es nochmal", "Tenta outra vez"),
            ["feedback.title"] = new("Bien joué !", "Ben fatto!", "Gut gemacht!", "Muito bem!"),
            ["feedback.no_mistakes"] = new("Aucune erreur", "Nessun errore", "Keine Fehler", "Sem erros"),
            ["feedback.one_mistake"] = new("1 errore" == "1 error" ? "1 erreur" : "1 erreur", "1 errore", "1 Fehler", "1 erro"),
            ["feedback.mistakes"] = new("{0} erreurs", "{0} errori", "{0} Fehler", "{0} erros"),
            ["feedback.continue"] = new("Continuer", "Continua", "Weiter", "Continuar"),

            ["minigame.mg1.prompt.0"] = new("<bounce>Leurs dénominateurs sont-ils comparables ?</bounce>", "<bounce>I loro denominatori sono confrontabili?</bounce>", "<bounce>Sind ihre Nenner vergleichbar?</bounce>", "<bounce>Os denominadores são comparáveis?</bounce>"),
            ["minigame.mg1.prompt.1"] = new("<bounce>Par quelle fraction devons-nous multiplier la première pour comparer les dénominateurs ?</bounce>", "<bounce>Per quale frazione dobbiamo moltiplicare la prima per rendere i denominatori confrontabili?</bounce>", "<bounce>Mit welchem Bruch müssen wir den ersten Bruch multiplizieren, damit die Nenner vergleichbar sind?</bounce>", "<bounce>Por que fração devemos multiplicar a primeira para tornar os denominadores comparáveis?</bounce>"),
            ["minigame.mg1.prompt.2"] = new("<bounce>Quel est le bon signe de comparaison ?</bounce>", "<bounce>Qual è il segno di confronto corretto?</bounce>", "<bounce>Welches Vergleichszeichen ist richtig?</bounce>", "<bounce>Qual é o sinal de comparação correto?</bounce>"),
            ["minigame.mg3.prompt.0"] = new("<bounce>Quelle fraction pourrait être équivalente aux morceaux de pain affichés ?</bounce>", "<bounce>Quale frazione potrebbe essere equivalente ai pezzi di pane mostrati?</bounce>", "<bounce>Welcher Bruch könnte den gezeigten Brotstücken entsprechen?</bounce>", "<bounce>Que fração pode ser equivalente aos pedaços de pão mostrados?</bounce>"),
            ["minigame.mg3.prompt.1"] = new("<bounce>Quelle fraction est plus grande que la part de dessert affichée ?</bounce>", "<bounce>Quale frazione è maggiore della porzione di dolce mostrata?</bounce>", "<bounce>Welcher Bruch ist größer als das gezeigte Dessertstück?</bounce>", "<bounce>Que fração é maior do que a porção de sobremesa mostrada?</bounce>"),
            ["minigame.mg3.prompt.2"] = new("<bounce>Quelle fraction est plus petite que la part de dessert affichée ?</bounce>", "<bounce>Quale frazione è minore della porzione di dolce mostrata?</bounce>", "<bounce>Welcher Bruch ist kleiner als das gezeigte Dessertstück?</bounce>", "<bounce>Que fração é menor do que a porção de sobremesa mostrada?</bounce>"),

            ["mg1.reset"] = new("Réinitialiser", "Ripristina", "Zurücksetzen", "Reiniciar"),
            ["mg1.understood"] = new("Compris", "Capito", "Verstanden", "Entendido"),
            ["mg1.yes"] = new("Oui", "Sì", "Ja", "Sim"),
            ["mg1.no"] = new("Non", "No", "Nein", "Não"),
            ["mg1.submit"] = new("Valider", "Invia", "Prüfen", "Verificar"),
            ["mg1.examples"] = new("Exemples : 2/2, 3/3, 4/4...", "Esempi: 2/2, 3/3, 4/4...", "Beispiele: 2/2, 3/3, 4/4...", "Exemplos: 2/2, 3/3, 4/4..."),
            ["mg1.equal_rule"] = new("Le numérateur et le dénominateur doivent être égaux", "Numeratore e denominatore devono essere uguali", "Zähler und Nenner müssen gleich sein", "Numerador e denominador devem ser iguais"),
            ["mg1.exp_title"] = new("Pour comparer des fractions, regardons d'abord les dénominateurs", "Per confrontare le frazioni, controlliamo prima i denominatori", "Um Brüche zu vergleichen, prüfen wir zuerst die Nenner", "Para comparar frações, verificamos primeiro os denominadores"),
            ["mg1.exp_1"] = new("1. S'ils sont égaux, on compare les numérateurs", "1. Se sono uguali, confrontiamo i numeratori", "1. Sind sie gleich, vergleichen wir die Zähler", "1. Se forem iguais, comparamos os numeradores"),
            ["mg1.exp_2"] = new("2. S'ils sont différents, on cherche un dénominateur commun", "2. Se sono diversi, cerchiamo un denominatore comune", "2. Sind sie verschieden, suchen wir einen gemeinsamen Nenner", "2. Se forem diferentes, procuramos um denominador comum"),
            ["mg1.exp_then"] = new("- Ensuite, on compare", "- Poi confrontiamo", "- Dann vergleichen wir", "- Depois comparamos"),
            ["mg1.instructions"] = new("<swing>1) Coupe le dessert le long de la ligne</swing>", "<swing>1) Taglia il dolce lungo la linea tratteggiata</swing>", "<swing>1) Schneide das Dessert entlang der Linie</swing>", "<swing>1) Corta a sobremesa pela linha pontilhada</swing>"),
            ["mg1.multiply_prompt"] = new("Par quelle fraction multiplier la première pour égaliser les dénominateurs ?", "Per quale frazione moltiplicare la prima per eguagliare i denominatori?", "Mit welchem Bruch den ersten multiplizieren, um die Nenner anzugleichen?", "Por que fração multiplicar a primeira para igualar os denominadores?"),

            ["mg2.question"] = new("Quelle est la valeur équivalente de ____ ?", "Qual è il valore equivalente di ____?", "Was ist der gleichwertige Wert von ____?", "Qual é o valor equivalente de ____?"),
            ["mg2.check"] = new("Vérifier", "Controlla", "Prüfen", "Verificar"),
            ["mg2.instructions"] = new("<swing>1) Glisse un signe sur l'assiette\n2) Touche Vérifier</swing>", "<swing>1) Trascina un segno sul piatto\n2) Tocca Controlla</swing>", "<swing>1) Ziehe ein Zeichen auf den Teller\n2) Tippe auf Prüfen</swing>", "<swing>1) Arrasta um sinal para o prato\n2) Toca em Verificar</swing>"),

            ["mg3.submit"] = new("Valider", "Invia", "Prüfen", "Verificar"),
            ["mg3.placeholder"] = new("Écris une fraction...", "Scrivi una frazione...", "Gib einen Bruch ein...", "Escreve uma fração..."),
            ["mg3.instructions"] = new("<swing>1) Écris la fraction\n2) Touche Valider</swing>", "<swing>1) Scrivi la frazione\n2) Tocca Invia</swing>", "<swing>1) Schreibe den Bruch\n2) Tippe auf Prüfen</swing>", "<swing>1) Escreve a fração\n2) Toca em Verificar</swing>"),
            ["mg3.initial_question"] = new("<bounce>Quelle est l'équivalence des parts affichées ?</bounce>", "<bounce>Qual è l'equivalenza dei pezzi mostrati?</bounce>", "<bounce>Was ist die Entsprechung der gezeigten Stücke?</bounce>", "<bounce>Qual é a equivalência dos pedaços mostrados?</bounce>"),

            // ── GamePhrases ──────────────────────────────────────────────────────
            ["bakery_mg1_intro_0"] = new("Bienvenue à la boulangerie de Slothy", "Benvenuto nella panetteria di Slothy", "Willkommen in Slothys Bäckerei", "Bem-vindo à padaria do Slothy"),
            ["bakery_mg1_intro_1"] = new("Je m'appelle Slothy et je suis le boulanger.", "Mi chiamo Slothy e sono il panettiere.", "Ich heiße Slothy und bin der Bäcker.", "Chamo-me Slothy e sou o padeiro."),
            ["bakery_mg1_intro_2"] = new("Merci d'avoir choisi la boulangerie de Slothy pour commencer.", "Grazie per aver scelto la panetteria di Slothy come tuo primo lavoro.", "Danke, dass du Slothys Bäckerei als deinen ersten Arbeitsplatz gewählt hast.", "Obrigado por escolheres a padaria do Slothy para começar."),
            ["bakery_mg1_intro_3"] = new("Aujourd'hui, j'ai besoin de ton aide pour comparer des parts de gâteaux et de desserts.", "Oggi ho bisogno del tuo aiuto per confrontare fette di torte e dolci.", "Heute brauche ich deine Hilfe beim Vergleichen von Kuchen- und Dessertstücken.", "Hoje preciso da tua ajuda para comparar fatias de bolos e sobremesas."),
            ["bakery_mg1_intro_4"] = new("J'aimerais donc t'expliquer un peu la magie de la comparaison des fractions.", "Vorrei quindi spiegarti un po' la magia del confronto tra frazioni.", "Deshalb möchte ich dir ein wenig über die Magie des Bruchrechnens erzählen.", "Por isso gostava de te explicar um pouco a magia de comparar frações."),
            ["bakery_mg1_intro_5"] = new("Tu peux comparer deux fractions en regardant leurs numérateurs et leurs dénominateurs.", "Puoi confrontare due frazioni controllando i loro numeratori e denominatori.", "Du kannst zwei Brüche vergleichen, indem du Zähler und Nenner prüfst.", "Podes comparar duas frações observando os numeradores e denominadores."),
            ["bakery_mg1_intro_6"] = new("Cela t'aide à savoir si une fraction est plus grande, plus petite ou égale à une autre.", "Questo ti aiuta a capire se una frazione è maggiore, minore o uguale a un'altra.", "So erkennst du, ob ein Bruch größer, kleiner oder gleich einem anderen ist.", "Isto ajuda-te a saber se uma fração é maior, menor ou igual a outra."),
            ["bakery_mg1_intro_7"] = new("Pour cette tâche, analyse les dénominateurs des fractions pour voir s'ils sont comparables.", "Per questo compito, analizza i denominatori delle frazioni per vedere se sono confrontabili.", "Analysiere für diese Aufgabe die Nenner der Brüche, um zu sehen, ob sie vergleichbar sind.", "Para esta tarefa, analisa os denominadores das frações para ver se são comparáveis."),
            ["bakery_mg1_intro_8"] = new("Je vais te donner un exemple.", "Ti farò un esempio.", "Ich gebe dir ein Beispiel.", "Vou dar-te um exemplo."),
            ["bakery_mg1_congrat_0"] = new("Super travail !", "Ottimo lavoro!", "Tolle Arbeit!", "Ótimo trabalho!"),
            ["bakery_mg1_congrat_1"] = new("Comme tu as pu le voir, on ne peut pas comparer 1/2 et 3/4 directement, donc on multiplie 1/2 par 2/2 pour obtenir 2/4.", "Come hai visto, non possiamo confrontare 1/2 e 3/4 direttamente, quindi moltiplichiamo 1/2 per 2/2 ottenendo 2/4.", "Wie du siehst, können wir 1/2 und 3/4 nicht direkt vergleichen, also multiplizieren wir 1/2 mit 2/2 zu 2/4.", "Como viste, não podemos comparar 1/2 e 3/4 diretamente, por isso multiplicamos 1/2 por 2/2 obtendo 2/4."),
            ["bakery_mg1_congrat_2"] = new("Les fractions 2/4 et 3/4 ont le même dénominateur, on peut donc comparer leurs numérateurs.", "Le frazioni 2/4 e 3/4 hanno lo stesso denominatore, quindi si confrontano i numeratori.", "Die Brüche 2/4 und 3/4 haben den gleichen Nenner und lassen sich über die Zähler vergleichen.", "As frações 2/4 e 3/4 têm o mesmo denominador, logo podemos comparar os numeradores."),
            ["bakery_mg1_congrat_3"] = new("Pratiquons encore un peu !", "Esercitiamoci ancora un po'!", "Üben wir noch ein bisschen!", "Vamos praticar mais um pouco!"),
            ["bakery_mg1_end_0"] = new("Travail incroyable !", "Lavoro incredibile!", "Fantastische Arbeit!", "Trabalho incrível!"),
            ["bakery_mg1_end_1"] = new("C'est la première étape : vérifier si les dénominateurs sont comparables ou égaux.", "Questo è il primo passo: controllare se i denominatori sono confrontabili o uguali.", "Das ist der erste Schritt: prüfen, ob die Nenner vergleichbar oder gleich sind.", "Este é o primeiro passo: verificar se os denominadores são comparáveis ou iguais."),
            ["bakery_mg1_end_2"] = new("Sinon, on les multiplie pour obtenir des dénominateurs comparables.", "Altrimenti, li moltiplichiamo per ottenere denominatori confrontabili.", "Wenn nicht, multiplizieren wir sie, um vergleichbare Nenner zu erhalten.", "Caso contrário, multiplicamo-las para obter denominadores comparáveis."),
            ["bakery_mg1_end_3"] = new("Ensuite, on compare les numérateurs pour trouver si c'est plus grand, plus petit ou égal.", "Poi confrontiamo i numeratori per capire se è maggiore, minore o uguale.", "Danach vergleichen wir die Zähler für größer, kleiner oder gleich.", "Depois comparamos os numeradores para ver se é maior, menor ou igual."),

            ["bakery_mg2_intro_0"] = new("Pour l'étape suivante, j'ai besoin de ton aide avec quelques commandes.", "Per il passo successivo, ho bisogno del tuo aiuto con alcuni ordini.", "Für den nächsten Schritt brauche ich deine Hilfe bei einigen Bestellungen.", "Para o passo seguinte, preciso da tua ajuda com alguns pedidos."),
            ["bakery_mg2_intro_1"] = new("Comme nous l'avons vu, il faut suivre quelques étapes pour comparer des fractions.", "Come abbiamo visto, dobbiamo seguire alcuni passaggi per confrontare le frazioni.", "Wie gesehen müssen wir beim Vergleichen von Brüchen einige Schritte befolgen.", "Como vimos, devemos seguir alguns passos para comparar frações."),
            ["bakery_mg2_intro_2"] = new("Vérifie leurs dénominateurs pour voir s'ils sont directement comparables.", "Controlla i denominatori per vedere se sono confrontabili direttamente.", "Prüfe ihre Nenner, ob sie direkt vergleichbar sind.", "Verifica os denominadores para ver se são comparáveis diretamente."),
            ["bakery_mg2_intro_3"] = new("S'ils sont égaux, les numérateurs indiquent quelle fraction est plus grande ou plus petite.", "Se sono uguali, i numeratori determinano quale frazione è maggiore o minore.", "Sind sie gleich, bestimmen die Zähler, welcher Bruch größer oder kleiner ist.", "Se forem iguais, os numeradores indicam qual fração é maior ou menor."),
            ["bakery_mg2_intro_4"] = new("S'ils sont différents, multiplie le numérateur et le dénominateur par une même valeur.", "Se non sono uguali, dobbiamo moltiplicare numeratore e denominatore per lo stesso valore.", "Sind sie verschieden, multipliziere Zähler und Nenner mit demselben Wert.", "Se forem diferentes, multiplicamos o numerador e o denominador pelo mesmo valor."),
            ["bakery_mg2_intro_5"] = new("Cela rendra la fraction comparable.", "Così la frazione diventerà confrontabile.", "Dadurch wird der Bruch vergleichbar.", "Assim a fração fica comparável."),
            ["bakery_mg2_intro_6"] = new("Dans cette tâche, nous devons suivre ces étapes.", "In questo compito dobbiamo seguire questi passaggi.", "Bei dieser Aufgabe müssen wir diese Schritte befolgen.", "Nesta tarefa, devemos seguir estes passos."),
            ["bakery_mg2_intro_7"] = new("Prends un crayon et du papier pour convertir les fractions si besoin.", "Prendi carta e penna e converti le frazioni se necessario.", "Nimm Papier und Stift und wandle die Brüche bei Bedarf um.", "Pega num lápis e papel e converte as frações se precisares."),
            ["bakery_mg2_intro_8"] = new("Puis glisse un des signes <, > ou = et appuie sur le bouton pour vérifier.", "Poi trascina uno dei segni <, > o = e premi il pulsante per verificare.", "Ziehe dann eines der Zeichen <, > oder = und drücke zum Prüfen auf den Button.", "Depois arrasta um dos sinais <, > ou = e toca no botão para verificar."),
            ["bakery_mg2_intro_9"] = new("Allons-y !", "Facciamolo!", "Los geht's!", "Vamos a isso!"),
            ["bakery_mg2_congrat_0"] = new("WAOUH ! Quel talent !", "WOW! Che abilità!", "WOW! Tolle Fähigkeiten!", "UAU! Que talento!"),
            ["bakery_mg2_congrat_1"] = new("La plus grande part était 1/3.", "La fetta più grande era 1/3.", "Das größere Stück war 1/3.", "A maior fatia era 1/3."),
            ["bakery_mg2_congrat_2"] = new("Comme tu le vois, 1/3 donne de plus grandes parts.", "Come puoi vedere, 1/3 ha fette più grandi.", "Wie du siehst, hat 1/3 größere Stücke.", "Como vês, 1/3 tem pedaços maiores."),
            ["bakery_mg2_congrat_3"] = new("Continuons à comparer des fractions !", "Continuiamo a confrontare le frazioni!", "Vergleichen wir weiter Brüche!", "Vamos continuar a comparar frações!"),
            ["bakery_mg2_end_0"] = new("Excellent travail !", "Ottimo lavoro!", "Ausgezeichnete Arbeit!", "Excelente trabalho!"),
            ["bakery_mg2_end_1"] = new("Tu as suivi les étapes parfaitement et tu maîtrises presque les fractions.", "Hai seguito i passaggi alla perfezione e ormai padroneggi il confronto tra frazioni.", "Du hast alle Schritte perfekt befolgt und beherrschst Brüche schon fast.", "Seguiste os passos na perfeição e quase dominas a comparação de frações."),
            ["bakery_mg2_end_2"] = new("Merci de m'avoir aidé pour cette tâche.", "Grazie per avermi aiutato con questo compito.", "Danke, dass du mir bei dieser Aufgabe geholfen hast.", "Obrigado por me ajudares nesta tarefa."),

            ["bakery_mg3_intro_0"] = new("Bien joué ! Tu es presque un expert des fractions de desserts et gâteaux.", "Ben fatto! Sei quasi un esperto nell'analizzare frazioni di dolci e torte.", "Gut gemacht! Du bist fast ein Experte für Kuchen- und Dessertbrüche.", "Bom trabalho! Já és quase um especialista em frações de sobremesas e bolos."),
            ["bakery_mg3_intro_1"] = new("Merci de m'avoir aidé pour les deux premières tâches.", "Grazie per avermi aiutato nei primi due compiti.", "Danke für deine Hilfe bei den ersten beiden Aufgaben.", "Obrigado por me ajudares nas duas primeiras tarefas."),
            ["bakery_mg3_intro_2"] = new("Pour cette étape, vérifions une dernière fois les commandes avant la livraison.", "Per questo passaggio, faremo un controllo finale degli ordini in consegna.", "In diesem Schritt prüfen wir ein letztes Mal die Bestellungen vor der Lieferung.", "Neste passo, vamos fazer a revisão final dos pedidos antes da entrega."),
            ["bakery_mg3_intro_3"] = new("Mettons tes compétences à l'épreuve encore une fois.", "Mettiamo alla prova le tue abilità ancora una volta.", "Stellen wir dein Können noch einmal auf die Probe.", "Vamos testar as tuas capacidades mais uma vez."),
            ["bakery_mg3_congrat_0"] = new("Incroyable !", "Incredibile!", "Unglaublich!", "Incrível!"),
            ["bakery_mg3_congrat_1"] = new("C'est exact !", "Hai ragione!", "Das ist richtig!", "Está certo!"),
            ["bakery_mg3_congrat_2"] = new("Pour mieux comprendre, on vérifie les dénominateurs et on voit s'ils sont comparables.", "Per capirlo meglio, controlliamo i denominatori e vediamo che non sono confrontabili.", "Zum besseren Verständnis prüfen wir die Nenner und sehen, ob sie vergleichbar sind.", "Para entender melhor, verificamos os denominadores e vemos que não são comparáveis."),
            ["bakery_mg3_congrat_3"] = new("On convertit 1/2 en multipliant son numérateur et son dénominateur par le même nombre.", "Convertiamo 1/2 moltiplicando numeratore e denominatore per lo stesso numero.", "Wir wandeln 1/2 um, indem wir Zähler und Nenner mit derselben Zahl multiplizieren.", "Convertemos 1/2 multiplicando o numerador e o denominador pelo mesmo número."),
            ["bakery_mg3_congrat_4"] = new("C'est ainsi qu'on obtient une fraction égale à 1/2.", "Così abbiamo ottenuto una frazione uguale a 1/2.", "So erhalten wir einen Bruch, der gleich 1/2 ist.", "Foi assim que obtivemos uma fração igual a 1/2."),
            ["bakery_mg3_congrat_5"] = new("Continuons.", "Continuiamo.", "Machen wir weiter.", "Vamos continuar."),
            ["bakery_mg3_end_0"] = new("Quel merveilleux travail !", "Che lavoro meraviglioso!", "Was für eine wunderbare Arbeit!", "Que trabalho maravilhoso!"),
            ["bakery_mg3_end_1"] = new("Après avoir comparé toutes ces fractions, je suis impressionné par tes connaissances.", "Dopo aver confrontato tutte queste frazioni, sono colpito dalla tua bravura.", "Nachdem du all diese Brüche verglichen hast, bin ich von deinem Wissen beeindruckt.", "Depois de comparares todas estas frações, estou impressionado com o teu conhecimento."),
            ["bakery_mg3_end_2"] = new("Je crois que tu mérites de devenir mon assistant étoile dès maintenant.", "Penso che tu meriti di diventare il mio assistente stella d'ora in poi.", "Ich denke, du hast es verdient, ab jetzt mein Stern-Assistent zu sein.", "Acho que mereces tornar-te o meu assistente estrela a partir de agora."),

            // ── Notifications ────────────────────────────────────────────────────
            ["bakery_notif_title"] = new("Slothy's Bakery s'ennuie de toi ! 🥐", "Slothy's Bakery sente la tua mancanza! 🥐", "Slothy's Bakery vermisst dich! 🥐", "A Slothy's Bakery sente a tua falta! 🥐"),
            ["bakery_notif_body"] = new("De nouveaux desserts sont prêts à comparer. Reviens aider Slothy !", "Nuovi dolci sono pronti da confrontare. Torna ad aiutare Slothy!", "Neue Desserts warten darauf, verglichen zu werden. Komm zurück und hilf Slothy!", "Há novas sobremesas prontas para comparar. Volta para ajudar o Slothy!"),

            // ── UI ───────────────────────────────────────────────────────────────
            ["rating_title"] = new("Tu aimes Slothy's Bakery ?", "Ti piace Slothy's Bakery?", "Gefällt dir Slothy's Bakery?", "Estás a gostar de Slothy's Bakery?"),
            ["rating_body"] = new("Ton avis nous aide à continuer d'améliorer le jeu.", "La tua recensione ci aiuta a migliorare il gioco.", "Deine Bewertung hilft uns, das Spiel weiter zu verbessern.", "A tua avaliação ajuda-nos a continuar a melhorar o jogo."),
            ["rating_btn_rate"] = new("⭐ Évaluer", "⭐ Valuta", "⭐ Bewerten", "⭐ Avaliar"),
            ["rating_btn_later"] = new("Plus tard", "Più tardi", "Später", "Mais tarde"),
            ["rating_btn_never"] = new("Non, merci", "No, grazie", "Nein, danke", "Não, obrigado"),
        };

        public static void EnsureSixLanguages()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/Localization/Localization Settings.asset");
            if (settings != null && LocalizationEditorSettings.ActiveLocalizationSettings == null)
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            Directory.CreateDirectory("Assets/Localization/Locale");
            foreach (string code in RequiredCodes)
            {
                var loc = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
                if (loc == null)
                {
                    loc = Locale.CreateLocale(code);
                    AssetDatabase.CreateAsset(loc, $"Assets/Localization/Locale/{code}.asset");
                    LocalizationEditorSettings.AddLocale(loc);
                }
            }

            if (settings != null)
            {
                var selectors = settings.GetStartupLocaleSelectors();
                if (!selectors.Any(s => s is PlayerPrefLocaleSelector))
                {
                    int idx = selectors.FindIndex(s => s is CommandLineLocaleSelector) + 1;
                    selectors.Insert(Mathf.Max(0, idx), new PlayerPrefLocaleSelector { PlayerPreferenceKey = "selected-locale" });
                    EditorUtility.SetDirty(settings);
                }
            }

            var locales = LocalizationEditorSettings.GetLocales();
            foreach (string colName in Collections)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(colName);
                if (collection == null) continue;

                foreach (var locale in locales)
                {
                    if (collection.GetTable(locale.Identifier) == null)
                        collection.AddNewTable(locale.Identifier);
                }

                var sharedKeys = collection.SharedData.Entries.Select(e => e.Key).ToList();
                var enTable = collection.GetTable(new LocaleIdentifier("en")) as StringTable;

                foreach (StringTable table in collection.StringTables)
                {
                    string shortCode = ShortCode(table.LocaleIdentifier.Code);
                    if (shortCode != "fr" && shortCode != "it" && shortCode != "de" && shortCode != "pt")
                        continue;

                    foreach (string key in sharedKeys)
                    {
                        string val = Translations.TryGetValue(key, out var row)
                            ? row.For(shortCode)
                            : null;
                        if (string.IsNullOrEmpty(val) && enTable != null)
                            val = enTable.GetEntry(key)?.LocalizedValue;
                        if (!string.IsNullOrEmpty(val))
                            table.AddEntry(key, val);
                    }
                    EditorUtility.SetDirty(table);
                }
                EditorUtility.SetDirty(collection.SharedData);
                EditorUtility.SetDirty(collection);
            }

            AssetDatabase.SaveAssets();
        }

        private static string ShortCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "en";
            string lower = code.ToLowerInvariant();
            if (lower.StartsWith("fr")) return "fr";
            if (lower.StartsWith("it")) return "it";
            if (lower.StartsWith("de")) return "de";
            if (lower.StartsWith("pt")) return "pt";
            if (lower.StartsWith("es")) return "es";
            return "en";
        }
    }
}
