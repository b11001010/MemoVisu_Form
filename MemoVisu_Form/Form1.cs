﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace MemoVisu_Form
{
    public partial class Form1 : Form
    {

        int edi;
        ArrayList eips = new ArrayList();
        ArrayList writeAddrs = new ArrayList();
        ArrayList readAddrs = new ArrayList();

        int margin_x = 0;       //ブロック同士の横の間隔
        int margin_y = 0;       //ブロック同士の縦の間隔
        int width = 3;          //ブロックの幅
        int height = 3;         //ブロックの高さ
        int row = 0x100;        //1行あたりのブロック数
        int offset = 0x3A0000;  //開始オフセット

        public Form1()
        {
            InitializeComponent();
        }

        //フォーム生成時
        private void Form1_Load(object sender, EventArgs e)
        {
            width_textBox.Text = width.ToString();
            height_textBox.Text = height.ToString();
            row_numericUpDown.Value = row;
            offset_textBox.Text = offset.ToString("X");
        }

        //「描画」ボタンクリック時
        private void button_paint_Click(object sender, EventArgs e)
        {
            writeSize_label.Text = "書き込みサイズ: " + writeAddrs.Count;
            int width = int.Parse(width_textBox.Text);
            int height = int.Parse(height_textBox.Text);

            //メインのImageオブジェクトを作成する
            Bitmap mainImg = new Bitmap(row * width + 10, 10000);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(mainImg);

            int x, y;
            foreach (int eip in eips) {
                int pos = eip - offset;	//オフセット分を引く
                x = pos % row;
                y = pos / row;
                x = x * margin_x + (x - 1) * width + 10;
                y = y * margin_y + (y - 1) * height;
                //塗りつぶされた長方形を描画する
                Brush b = new SolidBrush(Color.FromArgb(0x10, Color.Red));
                g.FillRectangle(b, x, y, width, height);
            }

            foreach (int addr in writeAddrs)
            {
                int pos = addr - offset;
                x = pos % row;
                y = pos / row;
                x = x * margin_x + (x - 1) * width + 10;
                y = y * margin_y + (y - 1) * height;
                //塗りつぶされた長方形を描画する
                Brush b = new SolidBrush(Color.FromArgb(0x7F, Color.Green));
                g.FillRectangle(b, x, y, width, height);
            }

            //リソースを解放する
            g.Dispose();
            //作成した画像を表示する
            mainPictureBox.Image = mainImg;


            //縮小版のImageオブジェクトを作成する
            Bitmap scaleDownImg = new Bitmap(picture_map.Width, picture_map.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g2 = Graphics.FromImage(scaleDownImg);
            
            //画像のサイズを縮小して描画する
            g2.DrawImage(mainImg, 0, 0, picture_map.Width, picture_map.Height);
            //Graphicsオブジェクトのリソースを解放する
            g2.Dispose();
            //PictureBox1に表示する
            picture_map.Image = scaleDownImg;
        }

        //「ファイルを開く」ボタンをクリック
        private void file_open_Click(object sender, EventArgs e)
        {
            //OpenFileDialogクラスのインスタンスを作成
            OpenFileDialog ofd = new OpenFileDialog();
            
            //はじめに「ファイル名」で表示される文字列を指定する
            ofd.FileName = "rtrace.txt";
            //はじめに表示されるフォルダを指定する
            ofd.InitialDirectory = @"";
            //[ファイルの種類]に表示される選択肢を指定する
            ofd.Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
            //[ファイルの種類]ではじめに「テキストファイル」が選択されているようにする
            ofd.FilterIndex = 1;
            //タイトルを設定する
            ofd.Title = "開くファイルを選択してください";
            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            ofd.RestoreDirectory = true;

            //ダイアログを表示する
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき選択されたファイルを読み取り専用で開く
                Stream stream;
                stream = ofd.OpenFile();
                if (stream != null)
                {
                    //内容を読み込み
                    string line = "";
                    StreamReader sr = new StreamReader(stream);
                    while ((line = sr.ReadLine()) != null)
                    {
                        try
                        {
                            //EIPをリストに格納
                            string eipString = line.Substring(0, 8);
                            eips.Add(Convert.ToInt32(eipString, 16));

                            //レジスタの値を更新
                            string regString = line.Substring(line.Length - 12, 12);
                            if ("EDI" == regString.Substring(0,3))
                            {
                                edi = Convert.ToInt32(regString.Substring(regString.Length - 8, 8), 16);
                            }
                        }
                        catch {/* nothing */}

                        //正規表現でメモリアクセス命令を判別
                        Regex r = new Regex(@"MOV (BYTE|WORD|DWORD) PTR DS:\[(..|...)\].*");
                            Match match = r.Match(line);
                            if (match.Success)
                            {
                                if(match.Groups[2].Value == "EDI")
                                {
                                    writeAddrs.Add(edi);
                                    layer_listBox.Items.Add(edi.ToString("X"));
                            }
                            }
                    }
                    //閉じる
                    sr.Close();
                    stream.Close();
                }
            }
        }

        //「折り返し幅」NumericUpDownの値の変更時
        private void row_numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            row = Decimal.ToInt32(row_numericUpDown.Value);
        }

        //「offset」テキストボックスの値変更時
        private void offset_textBox_TextChanged(object sender, EventArgs e)
        {
            offset = Convert.ToInt32(offset_textBox.Text, 16);
        }
    }
}