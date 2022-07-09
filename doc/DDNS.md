# DDNS 开机启动计划任务示例

> 以下操作是在 Ubuntu 系统，Windows 系统可通过“任务计划程序”进行类似操作。

1. 首先，前往仓库的 releases 下载程序上传到设备，然后添加执行权限。

2. 按照说明编写自己的配置文件

3. 编写开机启动服务

```bash
sudo vi /etc/systemd/system/ddns.service
```

文件内容如下：

```ini
[Unit]
Description=SangServerTool DDNS
After=network.target
ConditionPathExists=/home/sangsq/.tools/SangServerTool
 
[Service]
Type=forking
ExecStart=/home/sangsq/.tools/SangServerTool ddns -c /home/sangsq/.tools/config.json --v6=1 --delay=30
TimeoutSec=0
StandardOutput=journal+console
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
```

`ConditionPathExists` 为刚上传的程序文件地址，当其存在这个服务才会启动

`ExecStart` 这里要写程序和配置文件的全路径，在这里我用的是 IPv6 地址进行解析。保险起见，服务启动后延迟 30 秒后开始执行，主要是接口查询需要访问阿里云服务器，刚启动的时候，直接运行可能会报 DNS 解析的错误，也许使用 `After=network-online.target` 会解决，不过没有测试这个。

4. 设置开机启动服务

```bash
sudo systemctl enable ddns.service
```

5. 添加计划任务

除了开机启动外，我们也可以通过计划任务，半个小时执行以下程序，检查 IP 是否有变化。

```
sudo crontab -e
```

添加计划任务

```
*/30  *  *  *  * /home/sangsq/.tools/SangServerTool ddns -c /home/sangsq/.tools/config.json --v6=1
```

这里去除了延迟的检测，因为不是刚开机了。
