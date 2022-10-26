﻿/*
 *      [LKY Common Tools] Copyright (C) 2022 liukaiyuan@sjtu.edu.cn Inc.
 *      
 *      FileName : Lib_OfficeInstall.cs
 *      Developer: liukaiyuan@sjtu.edu.cn (Odysseus.Yuan)
 */

using LKY_OfficeTools.Common;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LKY_OfficeTools.Lib
{
    /// <summary>
    /// Office 安装类库
    /// </summary>
    internal class Lib_OfficeInstall
    {
        /// <summary>
        /// 重载实现安装
        /// </summary>
        internal Lib_OfficeInstall()
        {
            //下载后，开始安装
            int DownCode = Lib_OfficeDownload.FilesDownload();
            
            //判断下载情况
            switch (DownCode)
            {
                case 1:
                    StartInstall();
                    return;
                case 0:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"     × 未能找到可用的 Office 安装文件！");
                    return;
                case -1:
                    //无需下载安装，自动退出
                    return;
            }
        }

        /// <summary>
        /// 开始安装 Office
        /// </summary>
        internal static bool StartInstall()
        {
            //定义ODT文件位置
            string ODT_path_root = Environment.CurrentDirectory + @"SDK\ODT\";
            string ODT_path_exe = ODT_path_root + @"ODT.exe";
            string ODT_path_xml = ODT_path_root + @"config.xml";

            //检查ODT文件是否存在
            if (!File.Exists(ODT_path_exe) || !File.Exists(ODT_path_exe))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"     × 目录：{ODT_path_root} 下文件丢失，请重新下载本软件！");
                return false;
            }

            //修改新的xml信息
            ///修改安装目录，安装目录为运行根目录
            bool isNewInstallPath = Com_FileOS.XML.SetValue(ODT_path_xml, "SourcePath", Environment.CurrentDirectory);

            //检查是否修改成功（安装目录）
            if (!isNewInstallPath)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"     × 配置 Install 信息错误！");
                return false;
            }

            ///修改为新版本号
            bool isNewVersion = Com_FileOS.XML.SetValue(ODT_path_xml, "Version", Lib_OfficeInfo.latest_version.ToString());

            //检查是否修改成功（版本号）
            if (!isNewVersion)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"     × 配置 Version 信息错误！");
                return false;
            }

            //开始安装
            string install_args = $"/configure \"{ODT_path_xml}\"";     //配置命令行

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"\n------> 开始安装 Microsoft Office v{Lib_OfficeInfo.latest_version} ...");

            bool isInstallFinish = Com_ExeOS.RunExe(ODT_path_exe, install_args);

            //检查是否因配置不正确等导致，意外退出安装
            if (!isInstallFinish)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"     × Microsoft Office v{Lib_OfficeInfo.latest_version} 安装意外结束！");
                return false;
            }

            //检查注册表，判断安装是否成功
            RegistryKey HKLM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                    Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);      //判断操作系统版本（64位\32位）打开注册表项，不然 x86编译的本程序 读取 x64的程序会出现无法读取 已经存在于注册表 中的数据

            RegistryKey office_reg = HKLM.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");

            if (office_reg == null)
            {
                //找不到 ClickToRun 注册表
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"     × Microsoft Office v{Lib_OfficeInfo.latest_version} 安装失败！");
                return false;
            }
            else
            {
                object office_InstallVer = office_reg.GetValue("VersionToReport");
                if (office_InstallVer != null && office_InstallVer.ToString() == Lib_OfficeInfo.latest_version.ToString())      //必须先判断不为null，否则会抛出异常
                {
                    //一切正常
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"     √ Microsoft Office v{Lib_OfficeInfo.latest_version} 已安装完成。");
                    return true;
                }
                else
                {
                    //版本号和一开始下载的版本号不一致
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"     × 未能正确安装 Microsoft Office v{Lib_OfficeInfo.latest_version} 版本！");
                    return false;
                }
            } 
        }
    }
}