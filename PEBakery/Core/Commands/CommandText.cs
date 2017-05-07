﻿/*
    Copyright (C) 2016-2017 Hajin Jang
    Licensed under GPL 3.0
 
    PEBakery is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using PEBakery.Exceptions;
using PEBakery.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEBakery.Core.Commands
{
    public static class CommandText
    {
        public static List<LogInfo> TXTAddLine(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTAddLine));
            CodeInfo_TXTAddLine info = cmd.Info as CodeInfo_TXTAddLine;

            string fileName = StringEscaper.Preprocess(s, info.FileName);
            string line = StringEscaper.Preprocess(s, info.Line);
            string modeStr = StringEscaper.Preprocess(s, info.Mode);
            TXTAddLineMode mode;
            if (modeStr.Equals("Append", StringComparison.OrdinalIgnoreCase))
                mode = TXTAddLineMode.Append;
            else if (info.Mode.Equals("Prepend", StringComparison.OrdinalIgnoreCase))
                mode = TXTAddLineMode.Prepend;
            else
                throw new ExecuteException($"Mode [{modeStr}] must be one of [Append, Prepend]");

            // Detect encoding of text
            // If text does not exists, create blank file
            Encoding encoding = Encoding.UTF8;
            if (File.Exists(fileName))
                encoding = FileHelper.DetectTextEncoding(fileName);
            else
                FileHelper.WriteTextBOM(fileName, Encoding.UTF8);

            if (mode == TXTAddLineMode.Prepend)
            {
                string tempPath = FileHelper.CreateTempFile();
                using (StreamReader reader = new StreamReader(fileName, encoding))
                using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
                {
                    writer.WriteLine(line);
                    string lineFromSrc;
                    while ((lineFromSrc = reader.ReadLine()) != null)
                        writer.WriteLine(lineFromSrc);
                    reader.Close();
                    writer.Close();
                }
                FileHelper.FileReplaceEx(tempPath, fileName);

                logs.Add(new LogInfo(LogState.Success, $"Prepened [{line}] to [{fileName}]", cmd));
            }
            else if (mode == TXTAddLineMode.Append)
            {
                File.AppendAllText(fileName, line + "\r\n", encoding);
                logs.Add(new LogInfo(LogState.Success, $"Appended [{line}] to [{fileName}]", cmd));
            }

            return logs;
        }

        public static List<LogInfo> TXTAddLineOp(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTAddLineOp));
            CodeInfo_TXTAddLineOp infoOp = cmd.Info as CodeInfo_TXTAddLineOp;

            string fileName = StringEscaper.Preprocess(s, infoOp.InfoList[0].FileName);
            string modeStr = StringEscaper.Preprocess(s, infoOp.InfoList[0].Mode);
            TXTAddLineMode mode;
            if (modeStr.Equals("Append", StringComparison.OrdinalIgnoreCase))
                mode = TXTAddLineMode.Append;
            else if (modeStr.Equals("Prepend", StringComparison.OrdinalIgnoreCase))
                mode = TXTAddLineMode.Prepend;
            else
                throw new ExecuteException($"Mode [{modeStr}] must be one of [Append, Prepend]");

            List<string> prepLines = new List<string>();
            foreach (CodeInfo_TXTAddLine info in infoOp.InfoList)
            {
                string line = StringEscaper.Preprocess(s, info.Line);
                prepLines.Add(line);
            }

            StringBuilder b = new StringBuilder();
            foreach (var line in prepLines)
                b.AppendLine(line);
            string linesToWrite = b.ToString();

            // Detect encoding of text
            // If text does not exists, create blank file
            Encoding encoding = Encoding.UTF8;
            if (File.Exists(fileName))
                encoding = FileHelper.DetectTextEncoding(fileName);
            else
                FileHelper.WriteTextBOM(fileName, Encoding.UTF8);

            if (mode == TXTAddLineMode.Prepend)
            {
                string tempPath = FileHelper.CreateTempFile();
                using (StreamReader reader = new StreamReader(fileName, encoding))
                using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
                {
                    writer.Write(linesToWrite);
                    string lineFromSrc;
                    while ((lineFromSrc = reader.ReadLine()) != null)
                        writer.WriteLine(lineFromSrc);
                    reader.Close();
                    writer.Close();
                }
                FileHelper.FileReplaceEx(tempPath, fileName);

                logs.Add(new LogInfo(LogState.Success, $"Lines prepened to [{fileName}] : {Environment.NewLine}{linesToWrite}", cmd));
            }
            else if (mode == TXTAddLineMode.Append)
            {
                File.AppendAllText(fileName, linesToWrite, encoding);

                logs.Add(new LogInfo(LogState.Success, $"Lines appended to [{fileName}] : {Environment.NewLine}{linesToWrite}", cmd));
            }

            return logs;
        }

        public static List<LogInfo> TXTReplace(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTReplace));
            CodeInfo_TXTReplace info = cmd.Info as CodeInfo_TXTReplace;

            string fileName = StringEscaper.Preprocess(s, info.FileName);
            string toBeReplaced = StringEscaper.Preprocess(s, info.ToBeReplaced);
            string replaceWith = StringEscaper.Preprocess(s, info.ReplaceWith);

            if (File.Exists(fileName) == false)
                throw new ExecuteException($"File [{fileName}] not exists");
            Encoding encoding = FileHelper.DetectTextEncoding(fileName);

            string tempPath = FileHelper.CreateTempFile();
            int i = 0;
            using (StreamReader reader = new StreamReader(fileName, encoding))
            using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
            {
                string lineFromSrc;
                while ((lineFromSrc = reader.ReadLine()) != null)
                {
                    lineFromSrc = lineFromSrc.Replace(toBeReplaced, replaceWith);
                    writer.WriteLine(lineFromSrc);
                    i++;
                }
                reader.Close();
                writer.Close();
            }
            FileHelper.FileReplaceEx(tempPath, fileName);

            logs.Add(new LogInfo(LogState.Success, $"Replaced [{toBeReplaced}] with [{replaceWith}] [{i}] times", cmd));

            return logs;
        }

        public static List<LogInfo> TXTDelLine(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTDelLine));
            CodeInfo_TXTDelLine info = cmd.Info as CodeInfo_TXTDelLine;

            string fileName = StringEscaper.Preprocess(s, info.FileName);
            string deleteIfBeginWith = StringEscaper.Preprocess(s, info.DeleteIfBeginWith);
            if (File.Exists(fileName) == false)
                throw new ExecuteException($"File [{fileName}] not exists");
            Encoding encoding = FileHelper.DetectTextEncoding(fileName);

            int i = 0;
            string tempPath = FileHelper.CreateTempFile();
            using (StreamReader reader = new StreamReader(fileName, encoding))
            using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
            {
                string lineFromSrc;
                while ((lineFromSrc = reader.ReadLine()) != null)
                {
                    if (lineFromSrc.StartsWith(deleteIfBeginWith, StringComparison.OrdinalIgnoreCase))
                    {
                        i++;
                        continue;
                    }                        
                    writer.WriteLine(lineFromSrc);
                }
                reader.Close();
                writer.Close();
            }
            FileHelper.FileReplaceEx(tempPath, fileName);

            logs.Add(new LogInfo(LogState.Success, $"Deleted [{i}] lines from [{fileName}]", cmd));

            return logs;
        }

        public static List<LogInfo> TXTDelLineOp(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTDelLineOp));
            CodeInfo_TXTDelLineOp infoOp = cmd.Info as CodeInfo_TXTDelLineOp;

            string fileName = StringEscaper.Preprocess(s, infoOp.InfoList[0].FileName);
            if (File.Exists(fileName) == false)
                throw new ExecuteException($"File [{fileName}] not exists");

            List<string> prepDeleteIfBeginWith = new List<string>();
            foreach (CodeInfo_TXTDelLine info in infoOp.InfoList)
            {
                string deleteIfBeginWith = StringEscaper.Preprocess(s, info.DeleteIfBeginWith);
                prepDeleteIfBeginWith.Add(deleteIfBeginWith);
            }

            Encoding encoding = FileHelper.DetectTextEncoding(fileName);
            
            int count = 0;
            string tempPath = FileHelper.CreateTempFile();
            using (StreamReader reader = new StreamReader(fileName, encoding))
            using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
            {
                string lineFromSrc;
                while ((lineFromSrc = reader.ReadLine()) != null)
                {
                    bool writeLine = true;
                    foreach (string deleteIfBeginWith in prepDeleteIfBeginWith)
                    {
                        if (lineFromSrc.StartsWith(deleteIfBeginWith, StringComparison.OrdinalIgnoreCase))
                        {
                            writeLine = false;
                            count++;
                            break;
                        }
                    }
                    
                    if (writeLine)
                        writer.WriteLine(lineFromSrc);
                }
                reader.Close();
                writer.Close();
            }
            FileHelper.FileReplaceEx(tempPath, fileName);

            logs.Add(new LogInfo(LogState.Success, $"Deleted [{count}] lines from [{fileName}]", cmd));

            return logs;
        }

        public static List<LogInfo> TXTDelSpaces(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTDelSpaces));
            CodeInfo_TXTDelSpaces info = cmd.Info as CodeInfo_TXTDelSpaces;

            string fileName = StringEscaper.Preprocess(s, info.FileName);

            if (File.Exists(fileName) == false)
                throw new ExecuteException($"File [{fileName}] not exists");
            Encoding encoding = FileHelper.DetectTextEncoding(fileName);

            int i = 0;
            string tempPath = FileHelper.CreateTempFile();
            using (StreamReader reader = new StreamReader(fileName, encoding))
            using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
            {
                string lineFromSrc;
                while ((lineFromSrc = reader.ReadLine()) != null)
                {
                    int count = FileHelper.CountStringOccurrences(lineFromSrc, " ");
                    if (0 < count)
                    {
                        i++;
                        lineFromSrc = lineFromSrc.Replace(" ", string.Empty);
                    }
                    writer.WriteLine(lineFromSrc);
                }
                reader.Close();
                writer.Close();
            }
            FileHelper.FileReplaceEx(tempPath, fileName);

            logs.Add(new LogInfo(LogState.Success, $"Deleted [{i}] spaces", cmd));

            return logs;
        }

        public static List<LogInfo> TXTDelEmptyLines(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = new List<LogInfo>();

            Trace.Assert(cmd.Info.GetType() == typeof(CodeInfo_TXTDelEmptyLines));
            CodeInfo_TXTDelEmptyLines info = cmd.Info as CodeInfo_TXTDelEmptyLines;

            string fileName = StringEscaper.Preprocess(s, info.FileName);

            if (File.Exists(fileName) == false)
                throw new ExecuteException($"File [{fileName}] not exists");
            Encoding encoding = FileHelper.DetectTextEncoding(fileName);

            int i = 0;
            string tempPath = FileHelper.CreateTempFile();
            using (StreamReader reader = new StreamReader(fileName, encoding))
            using (StreamWriter writer = new StreamWriter(tempPath, false, encoding))
            {
                string lineFromSrc;
                while ((lineFromSrc = reader.ReadLine()) != null)
                {
                    if (lineFromSrc.Equals(string.Empty, StringComparison.Ordinal))
                        i++;
                    else
                        writer.WriteLine(lineFromSrc);
                }
                reader.Close();
                writer.Close();
            }
            FileHelper.FileReplaceEx(tempPath, fileName);

            logs.Add(new LogInfo(LogState.Success, $"Deleted [{i}] empty lines", cmd));

            return logs;
        }
    }
}