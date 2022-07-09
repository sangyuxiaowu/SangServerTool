# SSL计划任务示例

> 以下操作是在 Ubuntu 系统，Windows 系统可通过“任务计划程序”进行类似操作。

1. 修改好配置文件 

特别注意：如果服务器不能热加载证书，记得在配置文件配置好 `okshell` ，来实现 web 服务器的重启。 

2. 添加计划任务

```
sudo crontab -e
```

 添加计划任务

```
0  0  *  *  * /home/sangsq/.tools/SangServerTool ssl -c /home/sangsq/.tools/config.json
```

如果要处理多个域名，则需多个配置文件和计划任务。