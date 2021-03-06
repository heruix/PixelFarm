﻿//Apache2, 2014-present, WinterDev

using System;
using System.Collections.Generic;
using System.Text;
using LayoutFarm.TextEditing.Commands;
namespace LayoutFarm.TextEditing
{

    public partial class InternalTextLayerController
    {
        public void ReplaceCurrentLineTextRun(IEnumerable<EditableRun> textruns)
        {
            _textLineWriter.ReplaceCurrentLine(textruns);
        }

        public void ReplaceLocalContent(int nBackSpace, string content)
        {
            if (content != null)
            {
                for (int i = 0; i < nBackSpace; i++)
                {
                    DoBackspace();
                }
                //------------------
                int j = content.Length;
                if (j > 0)
                {
                    for (int i = 0; i < j; i++)
                    {
                        char c = content[i];
                        _textLineWriter.AddCharacter(c);
                        _commandHistoryList.AddDocAction(
                            new DocActionCharTyping(c, _textLineWriter.LineNumber, _textLineWriter.CharIndex));
                    }
                }
            }
        }
        public void AddUnformattedStringToCurrentLine(RootGraphic root, string str, TextSpanStyle initTextSpanStyle)
        {
            //this should be a text-service work ***
            //TODO: use specific text model to format this document
            using (System.IO.StringReader reader = new System.IO.StringReader(str))
            {
                string line = reader.ReadLine();
                List<EditableTextRun> runs = new List<EditableTextRun>();

                int lineCount = 0;
                while (line != null)
                {
                    if (lineCount > 0)
                    {
                        runs.Add(new EditableTextRun(root, '\n', initTextSpanStyle));
                    }

                    if (line.Length > 0)
                    {
                        runs.Add(new EditableTextRun(root, line, initTextSpanStyle));
                    }

                    //
                    line = reader.ReadLine();
                    lineCount++;
                }
                AddTextRunsToCurrentLine(runs.ToArray());
            }
        }
        public void AddTextRunsToCurrentLine(IEnumerable<EditableRun> textRuns)
        {
            VisualSelectionRangeSnapShot removedRange = RemoveSelectedText();
            int startLineNum = _textLineWriter.LineNumber;
            int startCharIndex = _textLineWriter.CharIndex;
            bool isRecordingHx = EnableUndoHistoryRecording;
            EnableUndoHistoryRecording = false;
            foreach (EditableRun t in textRuns)
            {
                if (t.IsLineBreak)
                {
                    _textLineWriter.SplitToNewLine();
                    CurrentLineNumber++;
                }
                else
                {
                    _textLineWriter.AddTextSpan(t);
                }
            }
            EnableUndoHistoryRecording = isRecordingHx;
            _commandHistoryList.AddDocAction(
                new DocActionInsertRuns(textRuns, startLineNum, startCharIndex,
                    _textLineWriter.LineNumber, _textLineWriter.CharIndex));
            _updateJustCurrentLine = false;
            //
            NotifyContentSizeChanged();
        }
        public void AddTextRunToCurrentLine(EditableRun t)
        {
            _updateJustCurrentLine = true;
            VisualSelectionRangeSnapShot removedRange = RemoveSelectedText();
            int startLineNum = _textLineWriter.LineNumber;
            int startCharIndex = _textLineWriter.CharIndex;
            bool isRecordingHx = EnableUndoHistoryRecording;
            EnableUndoHistoryRecording = false;
            if (t.IsLineBreak)
            {
                _textLineWriter.SplitToNewLine();
                CurrentLineNumber++;
            }
            else
            {
                _textLineWriter.AddTextSpan(t);
            }

            EnableUndoHistoryRecording = isRecordingHx;
            _commandHistoryList.AddDocAction(
                new DocActionInsertRuns(t, startLineNum, startCharIndex,
                    _textLineWriter.LineNumber, _textLineWriter.CharIndex));
            _updateJustCurrentLine = false;
            //
            NotifyContentSizeChanged();
        }
        public void CopyAllToPlainText(StringBuilder output)
        {
            _textLineWriter.CopyContentToStrignBuilder(output);
        }
        public void Clear()
        {
            //1.
            CancelSelect();
            _textLayer.Clear();
            _textLineWriter.Clear();
            //
            NotifyContentSizeChanged();
        }
        public void CopySelectedTextToPlainText(StringBuilder stBuilder)
        {
            if (_selectionRange != null)
            {
                _selectionRange.SwapIfUnOrder();
                if (_selectionRange.IsOnTheSameLine)
                {
                    List<EditableRun> copyRuns = new List<EditableRun>();
                    _textLineWriter.CopySelectedTextRuns(_selectionRange, copyRuns);
                    foreach (EditableRun t in copyRuns)
                    {
                        t.CopyContentToStringBuilder(stBuilder);
                    }
                }
                else
                {
                    VisualPointInfo startPoint = _selectionRange.StartPoint;
                    CurrentLineNumber = startPoint.LineId;
                    _textLineWriter.SetCurrentCharIndex(startPoint.LineCharIndex);
                    List<EditableRun> copyRuns = new List<EditableRun>();
                    _textLineWriter.CopySelectedTextRuns(_selectionRange, copyRuns);
                    foreach (EditableRun t in copyRuns)
                    {
                        t.CopyContentToStringBuilder(stBuilder);
                    }
                }
            }
        }
        public void CopyCurrentLine(StringBuilder output)
        {
            _textLineWriter.CopyLineContent(output);
        }
        public void CopyLine(int lineNum, StringBuilder output)
        {
            if (_textLineWriter.LineNumber == lineNum)
            {
                //on the sameline
                _textLineWriter.CopyLineContent(output);
            }
            else
            {
                int cur_line = _textLineWriter.LineNumber;
                _textLineWriter.MoveToLine(lineNum);
                _textLineWriter.CopyLineContent(output);
                _textLineWriter.MoveToLine(cur_line);
            }
            //backGroundTextLineWriter.MoveToLine(lineNum);
            //backGroundTextLineWriter.CopyLineContent(output);
        }

        public void StartSelect()
        {
            if (_textLineWriter != null)
            {
                _selectionRange = new VisualSelectionRange(GetCurrentPointInfo(), GetCurrentPointInfo());
            }
#if DEBUG
            if (dbugEnableTextManRecorder)
            {
                _dbugActivityRecorder.WriteInfo("TxLMan::StartSelect");
            }
#endif
        }
        public void EndSelect()
        {
            if (_textLineWriter != null)
            {
#if DEBUG
                if (dbugEnableTextManRecorder)
                {
                    _dbugActivityRecorder.WriteInfo("TxLMan::EndSelect");
                }
#endif
                _selectionRange.EndPoint = GetCurrentPointInfo();
            }
        }

        public void CancelSelect()
        {
#if DEBUG
            if (dbugEnableTextManRecorder)
            {
                _dbugActivityRecorder.WriteInfo("TxLMan::CancelSelect");
            }
#endif
            _selectionRange = null;
        }

        public void StartSelectIfNoSelection()
        {
            if (_selectionRange == null)
            {
                this.StartSelect();
            }
        }
        public void EndSelectIfNoSelection()
        {
            if (_selectionRange == null)
            {
                this.StartSelect();
            }
            this.EndSelect();
        }

        public VisualPointInfo FindTextRunOnPosition(int x, int y)
        {
            if (y < 0)
            {
                return null;
            }
            else
            {
                int j = _textLineWriter.LineCount;
                if (j > 0)
                {
                    EditableTextLine textLine = _textLineWriter.GetTextLineAtPos(y);
                    if (textLine != null)
                    {
                        return textLine.GetTextPointInfoFromCaretPoint(x);
                    }
                }
                return null;
            }
        }
    }
}