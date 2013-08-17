﻿using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LORD2
{
    public static class RTReader
    {
        private static Dictionary<string, Action<string[]>> _Commands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Action<string[]>> _DOCommands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase);

        private static int _CurrentLineNumber = 0;
        private static RTRFile _CurrentFile = null;
        private static RTRSection _CurrentSection = null;
        private static Dictionary<string, Int16> _GlobalI = new Dictionary<string, Int16>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalOther = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Int32> _GlobalP = new Dictionary<string, Int32>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalPLUS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, byte> _GlobalT = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Int32> _GlobalV = new Dictionary<string, Int32>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _GlobalWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static int _InBEGINCount = 0;
        private static bool _InCHOICE = false;
        private static List<string> _InCHOICEOptions = new List<string>();
        private static bool _InDOWrite = false;
        private static string _InGOTOHeader = "";
        private static int _InGOTOLineNumber = 0;
        private static int _InIFFalse = 999;
        private static bool _InSAY = false;
        private static bool _InSHOW = false;
        private static bool _InSHOWLOCAL = false;
        private static bool _InSHOWSCROLL = false;
        private static List<string> _InSHOWSCROLLLines = new List<string>();
        private static string _InWRITEFILE = "";
        private static Random _R = new Random();
        private static Dictionary<string, RTRFile> _RefFiles = new Dictionary<string, RTRFile>(StringComparer.OrdinalIgnoreCase);
        private static int _Version = 2;

        static RTReader()
        {
            // Initialize the commands dictionary
            _Commands.Add("@ADDCHAR", CommandADDCHAR);
            _Commands.Add("@BEGIN", CommandBEGIN);
            _Commands.Add("@BITSET", CommandBITSET);
            _Commands.Add("@BUSY", CommandBUSY);
            _Commands.Add("@BUSYMANAGER", CommandBUSYMANAGER);
            _Commands.Add("@CHECKMAIL", CommandCHECKMAIL);
            _Commands.Add("@CHOICE", CommandCHOICE);
            _Commands.Add("@CHOOSEPLAYER", CommandCHOOSEPLAYER);
            _Commands.Add("@CLEARBLOCK", CommandCLEARBLOCK);
            _Commands.Add("@CLEAR", CommandCLEAR);
            _Commands.Add("@CLOSESCRIPT", CommandCLOSESCRIPT);
            _Commands.Add("@CONVERT_FILE_TO_ANSI", CommandCONVERT_FILE_TO_ANSI);
            _Commands.Add("@CONVERT_FILE_TO_ASCII", CommandCONVERT_FILE_TO_ASCII);
            _Commands.Add("@DATALOAD", CommandDATALOAD);
            _Commands.Add("@DATANEWDAY", CommandDATANEWDAY);
            _Commands.Add("@DATASAVE", CommandDATASAVE);
            _Commands.Add("@DISPLAY", CommandDISPLAY);
            _Commands.Add("@DISPLAYFILE", CommandDISPLAYFILE);
            _Commands.Add("@DO", CommandDO);
            _Commands.Add("@DRAWMAP", CommandDRAWMAP);
            _Commands.Add("@DRAWPART", CommandDRAWPART);
            _Commands.Add("@END", CommandEND);
            _Commands.Add("@FIGHT", CommandFIGHT);
            _Commands.Add("@HALT", CommandHALT);
            _Commands.Add("@IF", CommandIF);
            _Commands.Add("@ITEMEXIT", CommandITEMEXIT);
            _Commands.Add("@KEY", CommandKEY);
            _Commands.Add("@LABEL", CommandLABEL);
            _Commands.Add("@LOADCURSOR", CommandLOADCURSOR);
            _Commands.Add("@LOADMAP", CommandLOADMAP);
            _Commands.Add("@LORDRANK", CommandLORDRANK);
            _Commands.Add("@NAME", CommandNAME);
            _Commands.Add("@OFFMAP", CommandOFFMAP);
            _Commands.Add("@OVERHEADMAP", CommandOVERHEADMAP);
            _Commands.Add("@PAUSEOFF", CommandPAUSEOFF);
            _Commands.Add("@PAUSEON", CommandPAUSEON);
            _Commands.Add("@READFILE", CommandREADFILE);
            _Commands.Add("@ROUTINE", CommandROUTINE);
            _Commands.Add("@RUN", CommandRUN);
            _Commands.Add("@SAVECURSOR", CommandSAVECURSOR);
            _Commands.Add("@SAVEGLOBALS", CommandSAVEGLOBALS);
            _Commands.Add("@SAY", CommandSAY);
            _Commands.Add("@SELLMANAGER", CommandSELLMANAGER);
            _Commands.Add("@SHOW", CommandSHOW);
            _Commands.Add("@SHOWLOCAL", CommandSHOWLOCAL);
            _Commands.Add("@UPDATE", CommandUPDATE);
            _Commands.Add("@VERSION", CommandVERSION);
            _Commands.Add("@WHOISON", CommandWHOISON);
            _Commands.Add("@WRITEFILE", CommandWRITEFILE);

            // Initialize the @DO commands dictionary
            // @DO <COMMAND> COMMANDS
            _DOCommands.Add("ADDLOG", CommandDO_ADDLOG);
            _DOCommands.Add("BEEP", CommandDO_BEEP);
            _DOCommands.Add("COPYTONAME", CommandDO_COPYTONAME);
            _DOCommands.Add("DELETE", CommandDO_DELETE);
            _DOCommands.Add("FRONTPAD", CommandDO_FRONTPAD);
            _DOCommands.Add("GETKEY", CommandDO_GETKEY);
            _DOCommands.Add("GOTO", CommandDO_GOTO);
            _DOCommands.Add("MOVE", CommandDO_MOVE);
            _DOCommands.Add("MOVEBACK", CommandDO_MOVEBACK);
            _DOCommands.Add("NUMRETURN", CommandDO_NUMRETURN);
            _DOCommands.Add("PAD", CommandDO_PAD);
            _DOCommands.Add("QUEBAR", CommandDO_QUEBAR);
            _DOCommands.Add("READCHAR", CommandDO_READCHAR);
            _DOCommands.Add("READNUM", CommandDO_READNUM);
            _DOCommands.Add("READSPECIAL", CommandDO_READSPECIAL);
            _DOCommands.Add("READSTRING", CommandDO_READSTRING);
            _DOCommands.Add("REPLACE", CommandDO_REPLACE);
            _DOCommands.Add("REPLACEALL", CommandDO_REPLACEALL);
            _DOCommands.Add("SAYBAR", CommandDO_SAYBAR);
            _DOCommands.Add("STRIP", CommandDO_STRIP);
            _DOCommands.Add("STRIPALL", CommandDO_STRIPALL);
            _DOCommands.Add("STRIPBAD", CommandDO_STRIPBAD);
            _DOCommands.Add("TRIM", CommandDO_TRIM);
            _DOCommands.Add("UPCASE", CommandDO_UPCASE);
            _DOCommands.Add("WRITE", CommandDO_WRITE);
            // @DO <SOMETHING> <COMMAND> COMMANDS
            _DOCommands.Add("/", CommandDO_DIVIDE);
            _DOCommands.Add("*", CommandDO_MULTIPLY);
            _DOCommands.Add("+", CommandDO_ADD);
            _DOCommands.Add("-", CommandDO_SUBTRACT);
            _DOCommands.Add("ADD", CommandDO_ADD);
            _DOCommands.Add("IS", CommandDO_IS);
            _DOCommands.Add("RANDOM", CommandDO_RANDOM);

            // Load all the ref files in the current directory
            LoadRefFiles(ProcessUtils.StartupPath);

            // Init global variables
            for (int i = 1; i <= 99; i++) _GlobalI.Add("`I" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 99; i++) _GlobalP.Add("`P" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 99; i++) _GlobalPLUS.Add("`+" + StringUtils.PadLeft(i.ToString(), '0', 2), "");
            for (int i = 1; i <= 10; i++) _GlobalS.Add("`S" + StringUtils.PadLeft(i.ToString(), '0', 2), "");
            for (int i = 1; i <= 99; i++) _GlobalT.Add("`T" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);
            for (int i = 1; i <= 40; i++) _GlobalV.Add("`V" + StringUtils.PadLeft(i.ToString(), '0', 2), 0);

            _GlobalOther.Add("`N", Door.DropInfo.Alias);
            _GlobalOther.Add("`E", "ENEMY"); // TODO
            _GlobalOther.Add("`G", (Door.DropInfo.Emulation == DoorEmulationType.ANSI ? "3" : "0"));
            _GlobalOther.Add("`X", " ");
            _GlobalOther.Add("`D", "\x08");
            _GlobalOther.Add("`1", Ansi.TextColor(Crt.Blue));
            _GlobalOther.Add("`2", Ansi.TextColor(Crt.Green));
            _GlobalOther.Add("`3", Ansi.TextColor(Crt.Cyan));
            _GlobalOther.Add("`4", Ansi.TextColor(Crt.Red));
            _GlobalOther.Add("`5", Ansi.TextColor(Crt.Magenta));
            _GlobalOther.Add("`6", Ansi.TextColor(Crt.Brown));
            _GlobalOther.Add("`7", Ansi.TextColor(Crt.LightGray));
            _GlobalOther.Add("`8", Ansi.TextColor(Crt.White)); // Supposed to be dark gray, but actually white
            _GlobalOther.Add("`9", Ansi.TextColor(Crt.LightBlue));
            _GlobalOther.Add("`0", Ansi.TextColor(Crt.LightGreen));
            _GlobalOther.Add("`!", Ansi.TextColor(Crt.LightCyan));
            _GlobalOther.Add("`@", Ansi.TextColor(Crt.LightRed));
            _GlobalOther.Add("`#", Ansi.TextColor(Crt.LightMagenta));
            _GlobalOther.Add("`$", Ansi.TextColor(Crt.Yellow));
            _GlobalOther.Add("`%", Ansi.TextColor(Crt.White));
            _GlobalOther.Add("`^", Ansi.TextColor(15));
            _GlobalOther.Add("`W", "TODO 1/10s");
            _GlobalOther.Add("`L", "TODO 1/2s");
            _GlobalOther.Add("`\\", "\r\n");
            _GlobalOther.Add("`r0", Ansi.TextBackground(Crt.Black));
            _GlobalOther.Add("`r1", Ansi.TextBackground(Crt.Blue));
            _GlobalOther.Add("`r2", Ansi.TextBackground(Crt.Green));
            _GlobalOther.Add("`r3", Ansi.TextBackground(Crt.Cyan));
            _GlobalOther.Add("`r4", Ansi.TextBackground(Crt.Red));
            _GlobalOther.Add("`r5", Ansi.TextBackground(Crt.Magenta));
            _GlobalOther.Add("`r6", Ansi.TextBackground(Crt.Brown));
            _GlobalOther.Add("`r7", Ansi.TextBackground(Crt.LightGray));
            _GlobalOther.Add("`c", Ansi.ClrScr() + "\r\n\r\n"); // TODO only `c works in RTReader, not `C -- bug, or should `C really not work?
            _GlobalOther.Add("`k", "TODO MORE");
            // TODO `b and `.

            _GlobalWords.Add("LOCAL", (Door.Local() ? "5" : "0"));
            _GlobalWords.Add("RESPONCE", "0");
            _GlobalWords.Add("RESPONSE", "0");
        }

        private static void AssignVariable(string variable, string value)
        {
            // Split while we still have the raw input string (in case we're doing a LENGTH operation)
            string[] values = value.Split(' ');

            // Translate the input string
            value = TranslateVariables(value);

            // Check for LENGTH operator
            if ((values.Length == 2) && (values[1].StartsWith("`")))
            {
                // TODO Both of these need to be corrected to match the docs
                if (values[0].ToUpper() == "LENGTH")
                {
                    values[0] = values[1].Length.ToString();
                }
                else if (values[0].ToUpper() == "REALLENGTH")
                {
                    values[0] = values[1].Length.ToString();
                }
            }
            else
            {
                // Translate the first split input variable, which is still raw (and may be used by number variables below)
                values[0] = TranslateVariables(values[0]);
            }

            // See which variables to update
            if (_GlobalI.ContainsKey(variable))
            {
                _GlobalI[variable] = Convert.ToInt16(values[0]);
            }
            if (_GlobalP.ContainsKey(variable))
            {
                _GlobalP[variable] = Convert.ToInt32(values[0]);
            }
            if (_GlobalPLUS.ContainsKey(variable)) _GlobalPLUS[variable] = value;
            if (_GlobalS.ContainsKey(variable)) _GlobalS[variable] = value;
            if (_GlobalT.ContainsKey(variable))
            {
                _GlobalT[variable] = Convert.ToByte(values[0]);
            }
            if (_GlobalV.ContainsKey(variable))
            {
                _GlobalV[variable] = Convert.ToInt32(values[0]);
            }
        }

        private static void CommandADDCHAR(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandBEGIN(string[] tokens)
        {
            _InBEGINCount += 1;
        }

        private static void CommandBITSET(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandBUSY(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandBUSYMANAGER(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandCHECKMAIL(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandCHOICE(string[] tokens)
        {
            _InCHOICEOptions.Clear();
            _InCHOICE = true;
        }

        private static void CommandCHOOSEPLAYER(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandCLEAR(string[] tokens)
        {
            // @CLEAR <screen or name or userscreen or text or picture or all>
            switch (tokens[1].ToUpper())
            {
                case "ALL":
                    CommandCLEAR("@CLEAR USERSCREEN".Split(' '));
                    CommandCLEAR("@CLEAR PICTURE".Split(' '));
                    CommandCLEAR("@CLEAR TEXT".Split(' '));
                    CommandCLEAR("@CLEAR NAME".Split(' '));
                    // TODO And redraws the screen
                    break;
                case "NAME":
                    Door.GotoXY(55, 15);
                    Door.Write(new string(' ', 22));
                    break;
                case "PICTURE":
                    for (int y = 3; y <= 13; y++)
                    {
                        Door.GotoXY(55, y);
                        Door.Write(new string(' ', 22));
                    }
                    break;
                case "SCREEN":
                    Door.ClrScr();
                    break;
                case "TEXT":
                    for (int y = 3; y <= 13; y++)
                    {
                        Door.GotoXY(32, y);
                        Door.Write(new string(' ', 22));
                    }
                    break;
                case "USERSCREEN":
                    for (int y = 16; y <= 23; y++)
                    {
                        Door.GotoXY(1, y);
                        Door.Write(new string(' ', 80));
                    }
                    Door.GotoXY(78, 23);
                    break;
                default:
                    LogMissing(tokens);
                    break;
            }
        }

        private static void CommandCLEARBLOCK(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandCLOSESCRIPT(string[] tokens)
        {
            // TODO How do we end the script now?  New variable?
        }

        private static void CommandCONVERT_FILE_TO_ANSI(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandCONVERT_FILE_TO_ASCII(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandDATALOAD(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandDATANEWDAY(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandDATASAVE(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandDISPLAY(string[] tokens)
        {
            Door.Write(TranslateVariables(string.Join("\r\n", _RefFiles[Path.GetFileNameWithoutExtension(tokens[3])].Sections[tokens[1]].Script.ToArray())));
        }

        private static void CommandDISPLAYFILE(string[] tokens)
        {
            // TODO As with WRITEFILE, don't allow for ..\..\blah
            // TODO Handle NOPAUSE and NOSKIP parameters
            Door.Write(FileUtils.FileReadAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(tokens[1])), RMEncoding.Ansi));
        }

        private static void CommandDO(string[] tokens)
        {
            if (_DOCommands.ContainsKey(tokens[1]))
            {
                _DOCommands[tokens[1]](tokens);
            }
            else if ((tokens.Length >= 3) && (_DOCommands.ContainsKey(tokens[2])))
            {
                _DOCommands[tokens[2]](tokens);
            }
            else
            {
                LogMissing(tokens);
            }
        }

        private static void CommandDO_ADD(string[] tokens)
        {
            if (tokens[2] == "+")
            {
                // @DO <number to change> + <change with what>
                AssignVariable(tokens[1], (Convert.ToInt32(TranslateVariables(tokens[1])) + Convert.ToInt32(TranslateVariables(tokens[3]))).ToString());
            }
            else if (tokens[2].ToUpper() == "ADD")
            {
                // DO <string var> ADD <string var or text>
                AssignVariable(tokens[1], TranslateVariables(tokens[1] + string.Join(" ", tokens, 3, tokens.Length - 3)));
            }
            else
            {
                LogMissing(tokens);
            }
        }

        private static void CommandDO_ADDLOG(string[] tokens)
        {
            // @DO ADDLOG the line under this will be added to LOGNOW.TXT
            LogMissing(tokens);
        }

        private static void CommandDO_BEEP(string[] tokens)
        {
            // @DO BEEP beep locally
            LogMissing(tokens);
        }

        private static void CommandDO_COPYTONAME(string[] tokens)
        {
            // @DO COPYTONAME store `S10 in `N
            _GlobalOther["`N"] = TranslateVariables("`S10");
        }

        private static void CommandDO_DELETE(string[] tokens)
        {
            // @DO DELETE <filename> delete the given file
            LogMissing(tokens);
        }

        private static void CommandDO_DIVIDE(string[] tokens)
        {
            // @DO <number to change> / <change with what>
            // TODO How to round?
            AssignVariable(tokens[1], (Convert.ToInt32(TranslateVariables(tokens[1])) / Convert.ToInt32(TranslateVariables(tokens[3]))).ToString());
        }

        private static void CommandDO_FRONTPAD(string[] tokens)
        {
            // @DO FRONTPAD <string variable> <length>
            LogMissing(tokens);
        }

        private static void CommandDO_GETKEY(string[] tokens)
        {
            // @DO GETKEY <String variable to put it in> IF A KEY IS NOT CURRENTLY BEING PRESSED, STORE _ AS RESULT
            if (Door.KeyPressed())
            {
                AssignVariable(tokens[2], Door.ReadKey().ToString());
            }
            else
            {
                AssignVariable(tokens[2], "_");
            }
        }

        private static void CommandDO_GOTO(string[] tokens)
        {
            // @DO GOTO <header or label>
            if (_CurrentFile.Sections.ContainsKey(tokens[2]))
            {
                // HEADER goto
                _InGOTOHeader = tokens[2];
            }
            else if (_CurrentSection.Labels.ContainsKey(tokens[2]))
            {
                // LABEL goto within current section
                _CurrentLineNumber = _CurrentSection.Labels[tokens[2]];
            }
            else
            {
                foreach (KeyValuePair<string, RTRSection> KVP in _CurrentFile.Sections)
                {
                    if (KVP.Value.Labels.ContainsKey(tokens[2]))
                    {
                        // LABEL goto within a different section
                        _InGOTOHeader = KVP.Key; // Section name
                        _InGOTOLineNumber = KVP.Value.Labels[tokens[2]];
                        break;
                    }
                }
            }
        }

        private static void CommandDO_IS(string[] tokens)
        {
            // @DO <Number To Change> IS <Change With What>
            // TODO @DO `s01 is getname 8
            // TODO @DO `p20 is deleted 8
            // TODO @DO <number variable> IS LENGTH <String variable>
            // TODO @DO <number variable> IS REALLENGTH <String variable>
            AssignVariable(tokens[1], string.Join(" ", tokens, 3, tokens.Length - 3));
        }

        private static void CommandDO_MOVE(string[] tokens)
        {
            // @DO MOVE <x> <y> a 0 means current position
            int X = Convert.ToInt32(TranslateVariables(tokens[2]));
            int Y = Convert.ToInt32(TranslateVariables(tokens[3]));
            if ((X > 0) && (Y > 0))
            {
                Door.GotoXY(X, Y);
            }
            else if (X > 0)
            {
                Door.GotoX(X);
            }
            else if (Y > 0)
            {
                Door.GotoY(Y);
            }
        }

        private static void CommandDO_MOVEBACK(string[] tokens)
        {
            // @DO MOVEBACK put player back to previous position
            LogMissing(tokens);
        }

        private static void CommandDO_MULTIPLY(string[] tokens)
        {
            // @DO <number to change> * <change with what>
            AssignVariable(tokens[1], (Convert.ToInt32(TranslateVariables(tokens[1])) * Convert.ToInt32(TranslateVariables(tokens[3]))).ToString());
        }

        private static void CommandDO_NUMRETURN(string[] tokens)
        {
            // @DO NUMRETURN <int var> <string var>
            string Translated = TranslateVariables(tokens[3]);
            string TranslatedWithoutNumbers = Regex.Replace(Translated, "[0-9]", "", RegexOptions.IgnoreCase);
            AssignVariable(tokens[2], (Translated.Length - TranslatedWithoutNumbers.Length).ToString());
        }

        private static void CommandDO_PAD(string[] tokens)
        {
            // @DO PAD <string variable> <length>
            LogMissing(tokens);
        }

        private static void CommandDO_QUEBAR(string[] tokens)
        {
            // @DO QUEBAR adds next line to saybar queue
            LogMissing(tokens);
        }

        private static void CommandDO_RANDOM(string[] tokens)
        {
            // @DO <Varible to put # in> RANDOM <Highest number> <number to add to it>
            int Min = Convert.ToInt32(tokens[4]);
            int Max = Min + Convert.ToInt32(tokens[3]);
            AssignVariable(tokens[1], _R.Next(Min, Max).ToString());
        }

        private static void CommandDO_READCHAR(string[] tokens)
        {
            // @DO READCHAR <string variable to put it in> 
            // TODO Door.ReadKey is nullable
            AssignVariable(tokens[2], Door.ReadKey().ToString());
        }

        private static void CommandDO_READNUM(string[] tokens)
        {
            // @DO READNUM <MAX LENGTH> <DEFAULT> (stores in `v40)
            string Default = "";
            if (tokens.Length >= 4) Default = TranslateVariables(tokens[3]);

            string ReadNum = Door.Input(Default, CharacterMask.Numeric, '\0', Convert.ToInt32(TranslateVariables(tokens[2])), Convert.ToInt32(TranslateVariables(tokens[2])), 31);
            int AnswerInt = 0;
            if (!int.TryParse(ReadNum, out AnswerInt)) AnswerInt = 0;

            AssignVariable("`V40", AnswerInt.ToString());
        }

        private static void CommandDO_READSPECIAL(string[] tokens)
        {
            // @DO READSPECIAL (String variable to put it in> <legal chars, 1st is default> prompt until one of legal chars is hit.  if enter is hit, it's same as hitting first char
            LogMissing(tokens);
        }

        private static void CommandDO_READSTRING(string[] tokens)
        {
            // @DO READSTRING <MAX LENGTH> <DEFAULT> <variable TO PUT IT IN> (variable may be left off, in which case store in `S10)
            string ReadString = Door.Input(Regex.Replace(TranslateVariables(tokens[3]), "NIL", "", RegexOptions.IgnoreCase), CharacterMask.All, '\0', Convert.ToInt32(TranslateVariables(tokens[2])), Convert.ToInt32(TranslateVariables(tokens[2])), 31);
            if (tokens.Length >= 5)
            {
                AssignVariable(tokens[4], ReadString);
            }
            else
            {
                AssignVariable("`S10", ReadString);
            }
        }

        private static void CommandDO_REPLACE(string[] tokens)
        {
            // @DO REPLACE <find> <replace> <in> replace first instance of FIND with REPLACE in IN
            // The following regex matches only the first instance of the word foo: (?<!foo.*)foo (from http://stackoverflow.com/a/148561/342378)
            // TODO Test that it does what it should
            AssignVariable(tokens[4], Regex.Replace(TranslateVariables(tokens[4]), "(?<!" + Regex.Escape(TranslateVariables(tokens[2])) + ".*)" + Regex.Escape(TranslateVariables(tokens[2])), TranslateVariables(tokens[3]), RegexOptions.IgnoreCase));
        }

        private static void CommandDO_REPLACEALL(string[] tokens)
        {
            // @DO REPLACEALL <find> <replace> <in> replace all instances of FIND with REPLACE in IN
            AssignVariable(tokens[4], Regex.Replace(TranslateVariables(tokens[4]), Regex.Escape(TranslateVariables(tokens[2])), TranslateVariables(tokens[3]), RegexOptions.IgnoreCase));
        }

        private static void CommandDO_SAYBAR(string[] tokens)
        {
            // @DO SAYBAR same as DO QUEBAR, but displays immediately
            LogMissing(tokens);
        }

        private static void CommandDO_STRIP(string[] tokens)
        {
            // @DO STRIP <string variable> (really trim)
            AssignVariable(tokens[2], TranslateVariables(tokens[2]).Trim());
        }

        private static void CommandDO_STRIPALL(string[] tokens)
        {
            // @DO STRIPALL (strips out all ` codes, useful for passwords apparently)
        }

        private static void CommandDO_STRIPBAD(string[] tokens)
        {
            // @DO STRIPBAD <string variable> (strip illegal ` and replaces via badwords.dat)
            LogMissing(tokens);
        }

        private static void CommandDO_SUBTRACT(string[] tokens)
        {
            // @DO <number to change> - <change with what>
            AssignVariable(tokens[1], (Convert.ToInt32(TranslateVariables(tokens[1])) - Convert.ToInt32(TranslateVariables(tokens[3]))).ToString());
        }

        private static void CommandDO_TRIM(string[] tokens)
        {
            // @DO TRIM <file name> <number to trim to> (remove lines from file until less than number in length)
            string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(tokens[2]));
            int MaxLines = Convert.ToInt32(TranslateVariables(tokens[3]));
            List<string> Lines = new List<string>();
            Lines.AddRange(FileUtils.FileReadAllLines(FileName, RMEncoding.Ansi));
            if (Lines.Count > MaxLines)
            {
                while (Lines.Count > MaxLines) Lines.RemoveAt(0);
                FileUtils.FileWriteAllLines(FileName, Lines.ToArray(), RMEncoding.Ansi);
            }
        }

        private static void CommandDO_UPCASE(string[] tokens)
        {
            // @DO UPCASE <string variable>
            AssignVariable(tokens[2], TranslateVariables(tokens[2]).ToUpper());
        }

        private static void CommandDO_WRITE(string[] tokens)
        {
            // @DO WRITE next one line is written to the screen, no line wrap
            _InDOWrite = true;
        }

        private static void CommandDRAWMAP(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandDRAWPART(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandEND(string[] tokens)
        {
            _InBEGINCount -= 1;
        }

        private static void CommandFIGHT(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandHALT(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandIF(string[] tokens)
        {
            bool Result = false;
            string Left = TranslateVariables(tokens[1]);
            string Right = TranslateVariables(tokens[3]);
            int LeftInt = 0;
            int RightInt = 0;

            switch (tokens[2].ToUpper())
            {
                case "EQUALS": // @IF <Varible> EQUALS <Thing the varible must be, or more or less then, or another varible>
                case "IS": // @IF <Varible> IS <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        Result = (LeftInt == RightInt);
                    }
                    else
                    {
                        Result = (Left == Right);
                    }
                    break;
                case "EXIST": // @IF <filename> EXIST <true or false>
                    string FileName = StringUtils.PathCombine(ProcessUtils.StartupPath, Left);
                    bool TrueFalse = Convert.ToBoolean(Right.ToUpper());
                    Result = (File.Exists(FileName) == TrueFalse);
                    break;
                case "INSIDE": // @IF <Word or variable> INSIDE <Word or variable>
                    Result = Right.ToUpper().Contains(Left.ToUpper());
                    break;
                case "LESS": // @IF <Varible> LESS <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        Result = (LeftInt < RightInt);
                    }
                    else
                    {
                        throw new ArgumentException("@IF LESS arguments were not numeric");
                    }
                    break;
                case "MORE": // @IF <Varible> MORE <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        Result = (LeftInt > RightInt);
                    }
                    else
                    {
                        throw new ArgumentException("@IF MORE arguments were not numeric");
                    }
                    break;
                case "NOT": // @IF <Varible> NOT <Thing the varible must be, or more or less then, or another varible>
                    if (int.TryParse(Left, out LeftInt) && int.TryParse(Right, out RightInt))
                    {
                        Result = (LeftInt != RightInt);
                    }
                    else
                    {
                        Result = (Left != Right);
                    }
                    break;
                default:
                    LogMissing(tokens);
                    break;
            }

            // Check if it's an IF block, or inline IF
            if (string.Join(" ", tokens).ToUpper().Contains("THEN DO"))
            {
                // @BEGIN..@END coming, so skip it if our result was false
                if (!Result) _InIFFalse = _InBEGINCount;
            }
            else
            {
                // Inline DO, so execute it
                if (Result)
                {
                    int DOOffset = (tokens[5].ToUpper() == "THEN") ? 6 : 5;
                    string[] DOtokens = ("@DO " + string.Join(" ", tokens, DOOffset, tokens.Length - DOOffset)).Split(' ');
                    CommandDO(DOtokens);
                }
            }
        }

        private static void CommandITEMEXIT(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandKEY(string[] tokens)
        {
            // TODO Handle positioning
            // TODO Also, does it erase after a keypress?
            // @KEY = "  `1[`!MORE`1]`7" from current cursor position
            // @KEY BOTTOM = "                                   `!<MORE>`7" on line 24
            // @KEY TOP =    "                                       `![`1MORE`!]`7" on line 15
            Door.Write(TranslateVariables("                                   `!<MORE>`7"));
            // TODO Erase after drawing
            Door.ReadKey();
        }

        private static void CommandLABEL(string[] tokens)
        {
            // Ignore
        }

        private static void CommandLOADCURSOR(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandLOADMAP(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandLORDRANK(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandNAME(string[] tokens)
        {
            // TODO Name.Length is going to include the ANSI sequences, so not be the correct length
            string Name = TranslateVariables(string.Join(" ", tokens, 1, tokens.Length - 1));
            if (Name.Length > 22) Name = Name.Substring(0, 22);
            Door.GotoXY(55 + ((22 - Name.Length) / 2), 15);
            Door.Write(Name);
        }

        private static void CommandOFFMAP(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandOVERHEADMAP(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandPAUSEOFF(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandPAUSEON(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandREADFILE(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandROUTINE(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandRUN(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandSAVECURSOR(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandSAVEGLOBALS(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandSAY(string[] tokens)
        {
            _InSAY = true;
        }

        private static void CommandSELLMANAGER(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandSHOW(string[] tokens)
        {
            if ((tokens.Length > 1) && (tokens[1].ToUpper() == "SCROLL"))
            {
                _InSHOWSCROLLLines.Clear();
                _InSHOWSCROLL = true;
            }
            else
            {
                _InSHOW = true;
            }
        }

        private static void CommandSHOWLOCAL(string[] tokens)
        {
            _InSHOWLOCAL = true;
        }

        private static void CommandUPDATE(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandVERSION(string[] tokens)
        {
            int RequiredVersion = Convert.ToInt32(tokens[1]);
            if (RequiredVersion > _Version) throw new ArgumentOutOfRangeException("VERSION", "@VERSION requested version " + RequiredVersion + ", we only support version " + _Version);
        }

        private static void CommandWHOISON(string[] tokens)
        {
            LogMissing(tokens);
        }

        private static void CommandWRITEFILE(string[] tokens)
        {
            // TODO Strip out any invalid filename characters?  (so for example they can't say ..\..\..\..\windows\system32\important_file.ext)
            _InWRITEFILE = StringUtils.PathCombine(ProcessUtils.StartupPath, TranslateVariables(tokens[1]));
        }

        public static void DisplayRefFileSections()
        {
            Door.ClrScr();
            Door.WriteLn("DEBUG OUTPUT");
            foreach (KeyValuePair<string, RTRFile> RefFile in _RefFiles)
            {
                Door.WriteLn("Ref File Name: " + RefFile.Key);
                foreach (KeyValuePair<string, RTRSection> Section in RefFile.Value.Sections)
                {
                    Door.WriteLn("  - " + Section.Key + " (" + Section.Value.Script.Count.ToString() + " lines)");
                }
            }
        }

        private static void EndCHOICE()
        {
            // @CHOICE next lines until next @ command are choice options in listbox.  RESPONCE and RESPONSE hold result, `V01 defines initial selected index
            /*The choice command is more useful now; you can now define *IF* type statements 
            so a certain choice will only be there if a conditional statement is met.
            For instance:
            @CHOICE
            Yes
            No
            =`p20 500 Hey, I have 500 exactly!
            !`p20 500 Hey, I have anything BUT 500 exactly!
            >`p20 500 Hey, I have MORE than 500!
            <`p20 100 Hey, I have LESS than 100!
            >`p20 100 <`p20 500 I have more then 100 and less than 500!
            Also:  You can check the status of individual bits in a `T player byte.  The 
            bit is true or false, like this:
            +`t12 1 Hey! Byte 12's bit 1 is TRUE! (which is 1)
            -`t12 3 Hey! Byte 12's bit 3 is FALSE! (which is 0)

            The = > and < commands can be stacked as needed.  In the above example, if 
            `p20 was 600, only options 1, 2, 4, and 5 would be available, and RESPONSE 
            would be set to the correct option if one of those were selected.  For 
            example, if `p20 was 600 and the user hit the selection:
            "Hey, I have more than 500", RESPONSE would be set to 5.*/

            // Output options
            Door.GotoXY(1, 16);
            int FirstKey = 65;
            for (int i = 0; i < _InCHOICEOptions.Count; i++)
            {
                Door.WriteLn(TranslateVariables("   `2(`0" + ((char)(FirstKey + i)).ToString() + "`2)`7 " + _InCHOICEOptions[i]));
            }

            // Get response
            char Choice = '\0';
            while (((byte)Choice < FirstKey) || ((byte)Choice > (FirstKey + _InCHOICEOptions.Count - 1)))
            {
                // TODO Door.ReadKey() is nullable
                Choice = Door.ReadKey().ToString().ToUpper()[0];
            }

            _GlobalWords["RESPONCE"] = (Choice - 64).ToString();
            _GlobalWords["RESPONSE"] = (Choice - 64).ToString();
        }

        private static void EndSHOWSCROLL()
        {
            // TODO This should be a scroll window, not just a dump and pause
            for (int i = 0; i < _InSHOWSCROLLLines.Count; i++)
            {
                Door.WriteLn(_InSHOWSCROLLLines[i]);
            }
            Door.ReadKey();
        }

        private static void LoadRefFile(string fileName)
        {
            // A place to store all the sections found in this file
            RTRFile NewFile = new RTRFile();

            // Where to store the info for the section we're currently working on
            string NewSectionName = "_HEADER";
            RTRSection NewSection = new RTRSection();

            // Loop through the file
            string[] Lines = FileUtils.FileReadAllLines(fileName, RMEncoding.Ansi);
            foreach (string Line in Lines)
            {
                string LineTrimmed = Line.Trim();

                // Check for new section
                if (LineTrimmed.StartsWith("@#"))
                {
                    // Store section in dictionary
                    NewFile.Sections.Add(NewSectionName, NewSection);

                    // Get new section name (presumes only one word headers allowed, trims @# off start) and reset script block
                    NewSectionName = Line.Trim().Split(' ')[0].Substring(2);
                    NewSection = new RTRSection();
                }
                else if (LineTrimmed.StartsWith("@LABEL "))
                {
                    NewSection.Script.Add(Line);

                    string[] Tokens = LineTrimmed.Split(' ');
                    NewSection.Labels.Add(Tokens[1].ToUpper(), NewSection.Script.Count - 1);
                }
                else
                {
                    // TODO Filter out comment lines, which should make the @IF followed by @BEGIN check more reliable, since @IF will never be followed by a comment if we filter it
                    // TODO Need to keep track of @DO DISPLAY and @SHOW and @CHOICE and @SAY statements though, so we don't filter out legitimate text!
                    NewSection.Script.Add(Line);
                }
            }

            // Store last open section in dictionary
            NewFile.Sections.Add(NewSectionName, NewSection);

            _RefFiles.Add(Path.GetFileNameWithoutExtension(fileName), NewFile);
        }

        private static void LoadRefFiles(string directoryName)
        {
            string[] RefFileNames = Directory.GetFiles(directoryName, "*.ref", SearchOption.TopDirectoryOnly);
            foreach (string RefFileName in RefFileNames)
            {
                LoadRefFile(RefFileName);
            }
        }

        private static void LogMissing(string[] tokens)
        {
            string Output = "TODO (hit a key): " + string.Join(" ", tokens);
            if (Output.Length > 80) Output = Output.Substring(0, 80);
            Crt.FastWrite(Output, 1, 25, 31);
            Crt.ReadKey();
            Crt.FastWrite(new string(' ', 80), 1, 25, 0);
        }

        public static void RunSection(string fileName, string sectionName)
        {
            // TODO What happens if invalid file and/or section name is given

            // Run the selected script
            _CurrentFile = _RefFiles[fileName];
            _CurrentSection = _CurrentFile.Sections[sectionName];
            _CurrentLineNumber = _InGOTOLineNumber;
            _InGOTOLineNumber = 0;
            RunScript(_CurrentSection.Script.ToArray());

            // If we exit this script with _InGOTOHeader set, it means we want to GOTO a different section
            if (_InGOTOHeader != "")
            {
                string TempGOTOHeader = _InGOTOHeader;
                _InGOTOHeader = "";
                RunSection(fileName, TempGOTOHeader);
            }
        }

        private static void RunScript(string[] script)
        {
            while (_CurrentLineNumber < script.Length)
            {
                string Line = script[_CurrentLineNumber];
                string LineTranslated = TranslateVariables(Line);
                string LineTrimmed = Line.Trim();

                if (_InIFFalse < 999)
                {
                    if (LineTrimmed.StartsWith("@"))
                    {
                        string[] Tokens = LineTrimmed.Split(' ');
                        switch (Tokens[0].ToUpper())
                        {
                            case "@BEGIN":
                                _InBEGINCount += 1;
                                break;
                            case "@END":
                                _InBEGINCount -= 1;
                                if (_InBEGINCount == _InIFFalse) _InIFFalse = 999;
                                break;
                        }
                    }
                }
                else
                {
                    if (LineTrimmed.StartsWith("@"))
                    {
                        if (_InCHOICE) EndCHOICE();
                        if (_InSHOWSCROLL) EndSHOWSCROLL();
                        _InCHOICE = false;
                        _InSAY = false;
                        _InSHOW = false;
                        _InSHOWLOCAL = false;
                        _InSHOWSCROLL = false;
                        _InWRITEFILE = "";

                        string[] Tokens = LineTrimmed.Split(' ');
                        if (_Commands.ContainsKey(Tokens[0]))
                        {
                            _Commands[Tokens[0]](Tokens);
                        }
                        else
                        {
                            LogMissing(Tokens);
                        }
                    }
                    else
                    {
                        // TODO If we're outputting something, we might need to do something here
                        if (_InCHOICE)
                        {
                            _InCHOICEOptions.Add(Line);
                        }
                        else if (_InDOWrite)
                        {
                            Door.Write(LineTranslated);
                            _InDOWrite = false;
                        }
                        else if (_InSAY)
                        {
                            // TODO SHould be in TEXT window
                            Door.Write(LineTranslated);
                        }
                        else if (_InSHOW)
                        {
                            Door.WriteLn(LineTranslated);
                        }
                        else if (_InSHOWLOCAL)
                        {
                            Ansi.Write(LineTranslated + "\r\n");
                        }
                        else if (_InSHOWSCROLL)
                        {
                            _InSHOWSCROLLLines.Add(LineTranslated);
                        }
                        else if (_InWRITEFILE != "")
                        {
                            FileUtils.FileAppendAllText(_InWRITEFILE, LineTranslated + Environment.NewLine, RMEncoding.Ansi);
                        }
                    }
                }

                // Check if we need to GOTO a header (and stop processing this script)
                if (_InGOTOHeader != "") return;

                _CurrentLineNumber += 1;
            }
        }

        private static string TranslateVariables(string input)
        {
            string inputUpper = input.ToUpper();

            if (inputUpper.Contains("`I"))
            {
                foreach (KeyValuePair<string, short> KVP in _GlobalI)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                }
            }
            if (inputUpper.Contains("`P"))
            {
                foreach (KeyValuePair<string, int> KVP in _GlobalP)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                }
            }
            if (inputUpper.Contains("`+"))
            {
                foreach (KeyValuePair<string, string> KVP in _GlobalPLUS)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
                }
            }
            if (inputUpper.Contains("`S"))
            {
                foreach (KeyValuePair<string, string> KVP in _GlobalS)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
                }
            }
            if (inputUpper.Contains("`T"))
            {
                foreach (KeyValuePair<string, byte> KVP in _GlobalT)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                }
            }
            if (inputUpper.Contains("`V"))
            {
                foreach (KeyValuePair<string, int> KVP in _GlobalV)
                {
                    input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value.ToString(), RegexOptions.IgnoreCase);
                }
            }
            foreach (KeyValuePair<string, string> KVP in _GlobalOther)
            {
                input = Regex.Replace(input, Regex.Escape(KVP.Key), KVP.Value, RegexOptions.IgnoreCase);
            }
            foreach (KeyValuePair<string, string> KVP in _GlobalWords)
            {
                // TODO This isn't correct, since for example one of the global words is LOCAL, and so if a message says "my local calling area is 519" it'll be replaced with "my 5 calling area is 519"
                // TODO It's good enough for now for me to test with though
                if (input.ToUpper() == KVP.Key.ToUpper())
                {
                    input = KVP.Value;
                }
                else
                {
                    input = Regex.Replace(input, Regex.Escape(" " + KVP.Key + " "), KVP.Value, RegexOptions.IgnoreCase);
                }
            }

            // TODO also translate language variables and variable symbols
            return input;
        }
    }
}
