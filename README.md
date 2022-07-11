# SangServerTool

包含两款工具：

- 服务器 DDNS 工具，用于内网服务动态域名解析，支持 IPv6
- 服务器 SSL 证书申请工具

目前仅支持阿里云，其他云服务的实现可以自行添加。

提供独立的 [linux-x64、osx-x64、linux-arm64、win-x64 下载](../../releases/latest)。其他平台可自行通过源码编译发布。 

这个服务的启动一般来说不需要一直运行。DDNS可以在设备开启时检测一次，以后每间隔一段时间检测一次，如一小时。

SSL证书申请，可以每天0点固定检查一次即可，将要过期时，程序会自动进行续期，更新证书。
注意 nginx 等服务需要重新加载一下证书，可配置 `Certificate:okshell` 来实现申请成功调用你指定的脚本文件。

[【DDNS 开机启动计划任务示例】](doc/DDNS.md) | [【SSL计划任务示例】](doc/SSL.md)

# 使用说明

## 配置文件

配置文件为json格式，需要使用时传入参数。两个功能可以复用一个配置文件，不同站点的 SSL 需要使用多个配置文件。

```json

{
  "Access": {
    "AK": "阿里云 AccessKeyId",  //AccessKeyId
    "SK": "阿里云 AccessKeySecret" //AccessKeySecret
  },
  "DDNS": {
    "ddns": "xxx.domain.com",  // DDNS要解析的域名
    "basedomain": "domain.com"  // 主域名
  },
  "Certificate": {
    "cerpath": "/usr/opt/ssl/domain.com.pem",  // 域名证书路径或要将新申请的证书放哪里
    "privatekey": "/usr/opt/ssl/domain.com.key", // 证书私钥路径或要将生成的私钥放哪里
    "domains": "*.dev.domain.com dev.domain.com domain.com ", // 证书的DNS Name，多个用空格隔开
    "basedomain": "domain.com", // 主域名
    "okshell": "/home/myname/.tools/restartnginx.sh" // 证书更新后执行的脚本文件
  },
  "ACME": {
    "email": "my@domain.com",  // ACME 申请证书的邮箱
    "account": "/etc/acme_account.pem"  // ACME 账户的私钥路径或要将其放在哪里
  },
  "CSR": {
    "CommonName": "domain.com",  // 证书的CSR信息
    "CountryName": "CN",
    "Locality": "BeiJing",
    "State": "BeiJing",
    "Organization": "Sang",
    "OrganizationUnit": "IT"
  }
}

```

## DDNS

参数说明：

| 参数 | 说明|
| --- | --- |
| -c, --config  | Required. Set config json file. <br> 设置配置文件路径 |
| --delay |  (Default: 0) How many seconds delay? <br> 启动后延迟多少秒进行检查处理，默认为 0，防止开机启动过早导致出现一些问题 |
| --del |  (Default: false) Is delete DDNS? <br>删除配置文件中设置的DDNS域名解析，默认为 false ，如果为 true，则尝试删除后退出 |
| --v6 | (Default: false) Is ipv6? <br>使用 IPv6 来解析，默认获取 IPv4 |
| --ip |  (Default: ) If set will be used. Otherwise automatically obtained.<br>You can set 'ifconfig', It will check from 'https://ifconfig.me/ip' to get you Internet IP. <br>默认为空字符，如果传入了指定 IP ，则使用这个 IP 来解析。<br>可以传入 'ifconfig' 值，该值则表示通过网络获取网络出口 IP 来解析

> 如：使用本地的 IPv6 进行 DDNS 设置

```bash
SangServerTool ddns -c "test.json" --v6=1
```

> 如：删除 DDNS 的域名解析

```bash
SangServerTool ddns -c "test.json" --del=1
```

该功能的配置文件使用 `Access` 和 `DDNS` 这两段。

```json

{
  "Access": {
    "AK": "阿里云 AccessKeyId",  //AccessKeyId
    "SK": "阿里云 AccessKeySecret" //AccessKeySecret
  },
  "DDNS": {
    "ddns": "xxx.domain.com",  // DDNS要解析的域名
    "basedomain": "domain.com"  // 主域名
  }
}
```

## SSL

参数说明：

| 参数 | 说明|
| --- | --- |
| -c, --config  | Required. Set config json file. <br> 设置配置文件路径 |
| --retry | (Default: 2) How many retries? <br> 验证域名时重试几次，默认2次 |
| --delay | (Default: 10) How many seconds to retry? <br> 验证域名时重试间隔多少秒，默认10秒 |

> 如：申请域名重试 3 次

```bash
SangServerTool ssl -c "test.json" --retry=3
```
该功能的配置文件使用 `Access` 、 `Certificate` 、 `ACME` 、`CSR` 

在配置 `Certificate` 信息时：

- 如果是新申请的只需要配置好证书 `cerpath` 和证书私钥 `privatekey` 的存放路径，程序会自行生成。若已经有证书会私钥配置好其位置会自行更新证书或使用当前已有的私钥。
- `domains` 支持多个域名，使用空格隔开
- `okshell` 证书更新后执行的脚本文件，如果服务器不能热加载证书，记得配置好，通过脚本文件进行重启服务

在配置 `ACME` 信息时：

- 如果第一次使用仅需要写上你的邮箱 `email` 和存放 ACME 账户的私钥文件位置 `account`，证书过期会收到邮件提醒
- 如果之前已有账户，可以使用已有的账户私钥，配置给  `account`

关于 `CSR` ，这段配不配都无所谓，毕竟是免费的证书，也不会生效，只是验证了域名的归属权。

# 支持

欢迎喜欢编程的朋友，关注我的微信公众号：桑榆肖物

![](https://open.weixin.qq.com/qr/code?username=gh_c874018d0317)
