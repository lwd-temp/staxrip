﻿Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

Imports StaxRip.UI

Public Class CodecComparisonForm
    Shared Property Pos As Integer

    Public CropLeft, CropTop, CropRight, CropBottom As Integer
    Public Zoom As Integer = 100

    Shadows Menu As ContextMenuStripEx

    Event UpdateMenu()

    Public Sub New()
        InitializeComponent()
        KeyPreview = True
        bnMenu.TabStop = False
        TabControl.AllowDrop = True

        Dim enabledFunc = Function() Not TabControl.SelectedTab Is Nothing
        Menu = New ContextMenuStripEx(Me)

        bnMenu.ContextMenuStrip = Menu
        TabControl.ContextMenuStrip = Menu

        Menu.Add("Add files to compare", AddressOf Add, Keys.O, Nothing, "Video files to compare, the file browser has multiselect enabled.")
        menu.Add("Close selected tab", AddressOf Remove, Keys.Delete, enabledFunc)
        Menu.Add("Save PNGs at current position", AddressOf Save, Keys.S, enabledFunc, "Saves a PNG image for every file/tab at the current position in the directory of the source file.")
        Menu.Add("Crop and Zoom", AddressOf CropZoom, Keys.C, enabledFunc)
        Menu.Add("Go To Frame", AddressOf GoToFrame, Keys.F, enabledFunc)
        Menu.Add("Go To Time", AddressOf GoToTime, Keys.T, enabledFunc)
        menu.Add("Select next tab", AddressOf NextTab, Keys.Space, enabledFunc)
        menu.Add("Navigate | 1 frame backward", Sub() TrackBar.Value -= 1, Keys.Left, enabledFunc)
        menu.Add("Navigate | 1 frame forward", Sub() TrackBar.Value += 1, Keys.Right, enabledFunc)
        menu.Add("Navigate | 100 frame backward", Sub() TrackBar.Value -= 100, Keys.Left Or Keys.Control, enabledFunc)
        menu.Add("Navigate | 100 frame forward", Sub() TrackBar.Value += 100, Keys.Right Or Keys.Control, enabledFunc)
        menu.Add("Help", AddressOf Me.Help, Keys.F1, Nothing)
    End Sub

    Sub Add()
        Using f As New OpenFileDialog
            f.SetFilter({"mkv", "mp4", "png", "webm", "m4a"})
            f.Multiselect = True
            f.SetInitDir(s.Storage.GetString("codec comparison folder"))

            If f.ShowDialog() = DialogResult.OK Then
                s.Storage.SetString("codec comparison folder", Filepath.GetDir(f.FileName))

                For Each i In f.FileNames
                    Add(i)
                Next
            End If
        End Using
    End Sub

    Private Sub Remove()
        If Not TabControl.SelectedTab Is Nothing Then
            Dim tab = TabControl.SelectedTab
            TabControl.TabPages.Remove(tab)
            tab.Dispose()
            RaiseEvent UpdateMenu()
        End If
    End Sub

    Private Sub Save()
        For Each i As VideoTab In TabControl.TabPages
            i.AVI.Position = Pos
            Dim outputPath = Filepath.GetDir(i.SourceFile) & Pos & " " + Filepath.GetBase(i.SourceFile) + ".png"

            Using b = i.GetBitmap
                b.Save(outputPath, ImageFormat.Png)
            End Using
        Next
    End Sub

    Sub Add(sourePath As String)
        Dim tab = New VideoTab()
        tab.Form = Me
        tab.Open(sourePath)
        TabControl.TabPages.Add(tab)
        DirectCast(TabControl.SelectedTab, VideoTab).TrackBarValueChanged()
        RaiseEvent UpdateMenu()
        Application.DoEvents()
    End Sub

    Private Sub TrackBar_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar.ValueChanged
        TrackBarValueChanged()
    End Sub

    Sub TrackBarValueChanged()
        If Not TabControl.SelectedTab Is Nothing Then
            DirectCast(TabControl.SelectedTab, VideoTab).TrackBarValueChanged()
            Pos = TrackBar.Value
        End If
    End Sub

    Sub Help()
        Dim f As New HelpForm()
        f.Doc.WriteStart(Text)
        f.Doc.WriteP("In the statistic tab of the x265 dialog choose Log Level Frame and enable CSV log file creation, the codec comparison tool can displays containing frame info.")
        f.Doc.WriteTips(Menu.GetTips)
        f.Doc.WriteTable("Shortcut Keys", Menu.GetKeys, False)
        f.Show()
    End Sub

    Private Sub NextTab()
        Dim index = TabControl.SelectedIndex + 1
        If index >= TabControl.TabPages.Count Then index = 0
        If index <> TabControl.SelectedIndex Then TabControl.SelectedIndex = index
    End Sub

    Sub Reload()
        For Each i As VideoTab In TabControl.TabPages
            i.Reload()
        Next
    End Sub

    Private Sub TabControl_Selected(sender As Object, e As TabControlEventArgs) Handles TabControl.Selected
        Dim tab = DirectCast(TabControl.SelectedTab, VideoTab)
        If Not tab Is Nothing Then tab.TrackBarValueChanged()
    End Sub

    Private Sub TabControl_Deselecting(sender As Object, e As TabControlCancelEventArgs) Handles TabControl.Deselecting
        For Each i As VideoTab In TabControl.TabPages
            i.AVI.Position = Pos
        Next
    End Sub

    Private Sub CodecComparisonForm_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Dispose()
    End Sub

    Private Sub CropZoom()
        Using f As New SimpleSettingsForm("Crop and Zoom")
            f.Size = New Size(500, 400)

            Dim ui = f.SimpleUI

            Dim page = ui.CreateFlowPage("main page")
            page.SuspendLayout()

            Dim nb = ui.AddNumericBlock(page)
            nb.Label.Text = "Crop Left:"
            nb.NumEdit.Init(0, 10000, 10)
            nb.NumEdit.Value = CropLeft
            nb.NumEdit.SaveAction = Sub(value) CropLeft = CInt(value)

            nb = ui.AddNumericBlock(page)
            nb.Label.Text = "Crop Top:"
            nb.NumEdit.Init(0, 10000, 10)
            nb.NumEdit.Value = CropTop
            nb.NumEdit.SaveAction = Sub(value) CropTop = CInt(value)

            nb = ui.AddNumericBlock(page)
            nb.Label.Text = "Crop Right:"
            nb.NumEdit.Init(0, 10000, 10)
            nb.NumEdit.Value = CropRight
            nb.NumEdit.SaveAction = Sub(value) CropRight = CInt(value)

            nb = ui.AddNumericBlock(page)
            nb.Label.Text = "Crop Bottom:"
            nb.NumEdit.Init(0, 10000, 10)
            nb.NumEdit.Value = CropBottom
            nb.NumEdit.SaveAction = Sub(value) CropBottom = CInt(value)

            ui.AddLine(page)

            nb = ui.AddNumericBlock(page)
            nb.Label.Text = "Zoom:"
            nb.NumEdit.Init(0, 1000, 10)
            nb.NumEdit.Value = Zoom
            nb.NumEdit.SaveAction = Sub(value) Zoom = CInt(value)

            page.ResumeLayout()

            If f.ShowDialog() = DialogResult.OK Then
                ui.Save()
                Reload()
                TrackBarValueChanged()
            End If
        End Using
    End Sub

    Private Sub GoToFrame()
        Dim value = InputBox.Show("Frame:", "Go To Frame", TrackBar.Value.ToString)
        Dim pos As Integer
        If Integer.TryParse(value, pos) Then TrackBar.Value = pos
    End Sub

    Private Sub GoToTime()
        Dim tab = DirectCast(TabControl.SelectedTab, VideoTab)
        Dim d As Date
        d = d.AddSeconds(tab.AVI.Position / tab.AVI.FrameRate)
        Dim value = InputBox.Show("Time:", "Go To Time", d.ToString("HH:mm:ss.fff"))
        Dim time As TimeSpan

        If value <> "" AndAlso TimeSpan.TryParse(value, time) Then
            TrackBar.Value = CInt((time.TotalMilliseconds / 1000) * tab.AVI.FrameRate)
        End If
    End Sub

    Class VideoTab
        Inherits TabPage

        Property AVI As AVIFile
        Property Form As CodecComparisonForm
        Property SourceFile As String

        Private FrameInfo As String()

        Sub New()
            SetStyle(ControlStyles.Opaque, True)
        End Sub

        Sub Reload()
            AVI.Dispose()
            Open(SourceFile)
        End Sub

        Sub Open(sourePath As String)
            Text = Filepath.GetBase(sourePath)
            SourceFile = sourePath

            Dim doc As New AviSynthDocument
            doc.Path = sourePath + ".avs"
            doc.Filters.Add(New AviSynthFilter("SetMemoryMax(512)"))

            If Filepath.GetExt(sourePath) = ".png" Then
                doc.Filters.Add(New AviSynthFilter("ImageSource(""" + sourePath + """, end = 0)"))
            Else
                doc.Filters.Add(New AviSynthFilter("FFVideoSource(""" + sourePath + """)"))
            End If

            If (Form.CropLeft Or Form.CropTop Or Form.CropRight Or Form.CropBottom) <> 0 Then
                doc.Filters.Add(New AviSynthFilter("Crop(" & Form.CropLeft & ", " & Form.CropTop & ", -" & Form.CropRight & ", -" & Form.CropBottom & ")"))
            End If

            If p.SourceHeight > 576 Then
                doc.Filters.Add(New AviSynthFilter("ConvertToRGB(matrix=""Rec709"")"))
            Else
                doc.Filters.Add(New AviSynthFilter("ConvertToRGB(matrix=""Rec601"")"))
            End If

            If Form.Zoom <> 100 Then
                doc.Filters.Add(New AviSynthFilter("LanczosResize(Int(width / 100.0 * " & Form.Zoom & "), Int(height / 100.0 * " & Form.Zoom & "))"))
            End If

            doc.Synchronize()
            AVI = New AVIFile(doc.Path)

            If Form.TrackBar.Maximum < AVI.FrameCount - 1 Then
                Form.TrackBar.Maximum = AVI.FrameCount - 1
            End If

            Dim csvFile = Filepath.GetDirAndBase(sourePath) + ".csv"

            If File.Exists(csvFile) Then
                Dim len = Form.TrackBar.Maximum
                Dim lines = File.ReadAllLines(csvFile)

                If lines.Length > len Then
                    FrameInfo = New String(len) {}
                    Dim headers = lines(0).Split({","c})

                    For x = 1 To len + 1
                        Dim values = lines(x).Split({","c})

                        For x2 = 0 To headers.Length - 1
                            Dim value = values(x2).Trim

                            If value <> "" AndAlso value <> "-" Then
                                FrameInfo(x - 1) += headers(x2).Trim + ": " + value + ", "
                            End If
                        Next

                        FrameInfo(x - 1) = FrameInfo(x - 1).TrimEnd(" ,".ToCharArray)
                    Next
                End If
            End If
        End Sub

        Sub Draw()
            Dim padding As Padding
            Dim sizeToFit = New Size(AVI.FrameSize.Width, AVI.FrameSize.Height)

            Dim rect As New Rectangle(padding.Left, padding.Top,
                                      Width - padding.Horizontal,
                                      Height - padding.Vertical)
            Dim targetPoint As Point
            Dim targetSize As Size
            Dim ar1 = rect.Width / rect.Height
            Dim ar2 = sizeToFit.Width / sizeToFit.Height

            If ar2 < ar1 Then
                targetSize.Height = rect.Height
                targetSize.Width = CInt(sizeToFit.Width / (sizeToFit.Height / rect.Height))
                targetPoint.X = CInt((rect.Width - targetSize.Width) / 2) + padding.Left
                targetPoint.Y = padding.Top
            Else
                targetSize.Width = rect.Width
                targetSize.Height = CInt(sizeToFit.Height / (sizeToFit.Width / rect.Width))
                targetPoint.Y = CInt((rect.Height - targetSize.Height) / 2) + padding.Top
                targetPoint.X = padding.Left
            End If

            Dim targetRect = New Rectangle(targetPoint, targetSize)
            Dim reg As New Region(ClientRectangle)
            reg.Exclude(targetRect)

            Using g = CreateGraphics()
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.FillRegion(Brushes.Black, reg)
                g.DrawImage(GetBitmap, targetRect)
            End Using
        End Sub

        Function GetBitmap() As Bitmap
            Dim ret = AVI.GetBitmap

            Using g = Graphics.FromImage(ret)
                Dim text = Filepath.GetBase(SourceFile)
                Dim fontSize = ret.Height \ 100
                If fontSize < 10 Then fontSize = 10
                Dim font = New Font("Arial", fontSize)
                Dim size = g.MeasureString(text, font)
                Dim rect = New RectangleF(font.Height \ 2, font.Height \ 2, size.Width, size.Height)
                g.FillRectangle(Brushes.DarkGray, rect)
                g.DrawString(text, font, Brushes.White, rect)
            End Using

            Return ret
        End Function

        Sub TrackBarValueChanged()
            AVI.Position = Form.TrackBar.Value

            Try
                Draw()
            Catch ex As Exception
                Form.Reload()

                Try
                    Draw()
                Catch ex2 As Exception
                    g.ShowException(ex2)
                End Try
            End Try

            If Not FrameInfo Is Nothing Then
                Form.lInfo.Text = FrameInfo(Form.TrackBar.Value)
            Else
                Dim d As Date
                d = d.AddSeconds(AVI.Position / AVI.FrameRate)
                Form.lInfo.Text = "Position: " & AVI.Position & ", Time: " + d.ToString("HH:mm:ss.fff") + ", Size: " & AVI.FrameSize.Width & " x " & AVI.FrameSize.Height
            End If

            Form.lInfo.Refresh()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Draw()
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            AVI.Dispose()
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Class