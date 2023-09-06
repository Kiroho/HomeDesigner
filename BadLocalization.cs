using System;
using System.Collections.Generic;
namespace EmoteTome
{
    class BadLocalization
    {
        public static readonly int ENGLISH = 0;
        public static readonly int FRENCH = 1;
        public static readonly int GERMAN = 2;
        public static readonly int SPANISH = 3;

        public static readonly List<String> WINDOWTITLE = new List<String> {
            "Emote Tome",
            "Emote Tome",
            "Emote Tome",
            "Emote Tome"
            };
        public static readonly List<String> UNLOCKABLEPANELTITLE = new List<String> {
            "Unlockable Emotes",
            "Emotes pour déverrouiller",
            "Freischaltbare Emotes",
            "Emotes desbloqueables"
            };
        public static readonly List<String> RANKPANELTITLE = new List<String> {
            "Rank Emotes",
            "Rang Emotes",
            "Rang Emotes",
            "Rango Emotes"
            };
        public static readonly List<String> COREPANELTITLE = new List<String> {
            "Core Emotes",
            "Émote de base",
            "Kern Emotes",
            "Emotes de base"
            };
        public static readonly List<String> NOEMOTEONKEYPRESSED = new List<String> {
            "Can't peform emotes while a key is pressed",
            "Impossible d'effectuer des emotes pendant qu'une touche est pressée",
            "Nicht möglich, während eine Taste gedrückt ist",
            "No se pueden realizar emotes mientras se pulsa una tecla"
            };
        public static readonly List<String> NOEMOTEWHENMOVING = new List<String> {
            "Can't peform emotes while moving",
            "Impossible d'effectuer des emotes en se déplaçant",
            "Während der Bewegung nicht möglich",
            "No se pueden realizar emotes en movimiento"
            };
        public static readonly List<String> NOEMOTEONMOUNT = new List<String> {
            "Can't peform emotes while on mount",
            "Impossible d'effectuer des emotes lorsque l'on est sur une monture",
            "Auf Mounts nicht möglich",
            "No se pueden realizar emotes mientras se está montado"
            };
        public static readonly List<String> NOEMOTEINCOMBAT = new List<String> {
            "Can't peform emotes while in combat",
            "Impossible d'effectuer des emotes pendant le combat",
            "Im Kampf nicht möglich",
            "No se pueden realizar emotes en combate"
            };
        public static readonly List<String> SYNCHRONCHECKBOXTEXT = new List<String> {
            "Synchronize emotes",
            "Synchroniser les emotes",
            "Emotes synchronisieren",
            "Sincronizar emotes"
            };
        public static readonly List<String> SYNCHRONCHECKBOXTOOLTIP = new List<String> {
            "Synchronizes your emotes with other players.",
            "Synchronise vos emotes avec celles des autres joueurs.",
            "Synchronisiert Dein Emote mit anderen Spielern.",
            "Sincroniza tus emotes con los de otros jugadores."
            };
        public static readonly List<String> TARGETCHECKBOXTEXT = new List<String> {
            "Enable Targeting",
            "Activer le ciblage",
            "Anvisieren aktivieren",
            "Activar objetivo"
            };
        public static readonly List<String> TARGETCHECKBOXTOOLTIP = new List<String> {
            "Peforme emote on target.\nSome emotes are not affected.",
            "Peforme emote on target.\nCertaines emotes ne sont pas affectées.",
            "Führt Emote an Ziel aus.\nBetrifft nicht alle Emotes.",
            "Peforme emote en target.\nAlgunos emotes no se ven afectados."
            };

    }
}
