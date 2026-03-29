# 🖥️ Setup da EC2 na AWS

Guia para preparar a instância EC2 para receber o deploy da API.

## 1. Criar a Instância EC2

### Console AWS
1. Acesse o [EC2 Dashboard](https://console.aws.amazon.com/ec2/)
2. Clique em **Launch Instance**
3. Configure:
   - **Name:** `sample-api-server`
   - **AMI:** Amazon Linux 2023 ou Ubuntu 22.04
   - **Instance type:** `t2.micro` (Free Tier) ou `t3.micro`
   - **IAM Instance Profile:** Role com acesso ao ECR, SSM e SSM Parameter Store
   - **Security Group:** Veja seção abaixo
   - **Tags obrigatórias:**
     - `AppName` = `api`
     - `Environment` = `dev` (ou `prod`)

> ⚠️ Não precisa de Key Pair para SSH — o acesso é via **SSM Session Manager**.

## 2. Security Group

Libere as seguintes portas:

| Tipo   | Protocolo | Porta | Origem        | Descrição         |
|--------|-----------|-------|---------------|-------------------|
| Custom | TCP       | 8080  | 0.0.0.0/0     | API               |
| HTTPS  | TCP       | 443   | 0.0.0.0/0     | SSM Agent (saída) |

> 💡 O SSM Agent se comunica via HTTPS (porta 443 de saída). Não precisa abrir porta SSH (22).

## 3. IAM Role da EC2

A instância precisa de uma IAM Role com as seguintes políticas:
- `AmazonSSMManagedInstanceCore` (para SSM Session Manager)
- `AmazonEC2ContainerRegistryReadOnly` (para pull de imagens do ECR)
- Acesso ao SSM Parameter Store (se usar `sync-env.sh`)

## 4. Instalar Docker e Docker Compose na EC2

Conecte via Session Manager (Console AWS → EC2 → Selecione a instância → Connect → Session Manager).

### Amazon Linux 2023
```bash
# Instalar Docker
sudo yum update -y
sudo yum install -y docker
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker ec2-user

# Instalar Docker Compose (v1)
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Verificar
docker --version
docker-compose --version
```

### Ubuntu 22.04
```bash
# Instalar Docker
sudo apt-get update
sudo apt-get install -y docker.io
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker ubuntu

# Instalar Docker Compose (v1)
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Verificar
docker --version
docker-compose --version
```

## 5. Preparar a pasta da aplicação

```bash
sudo mkdir -p /opt/app
sudo chown $(whoami):$(whoami) /opt/app
```

Na pasta `/opt/app` devem existir:
- `docker-compose.yml` — com a definição do serviço apontando para a imagem `latest` do ECR
- `sync-env.sh` — script que sincroniza variáveis do SSM Parameter Store para o `.env`

## 6. Configurar Secrets no GitHub

Configure no repositório (`Settings > Secrets and variables > Actions`):

```
AWS_ACCESS_KEY_ID     = <access key do IAM user>
AWS_SECRET_ACCESS_KEY = <secret key do IAM user>
ECR_REGISTRY          = <account_id>.dkr.ecr.us-east-1.amazonaws.com
```

> 💡 Não precisa de `EC2_HOST`, `EC2_USER` nem `EC2_SSH_KEY` — o deploy usa SSM e encontra a EC2 pelas tags automaticamente.

## 7. Após o Deploy

```bash
# Conectar via Session Manager
# (Console AWS → EC2 → Connect → Session Manager)

# Verificar se os containers estão rodando
cd /opt/app
docker-compose ps

# Ver logs
docker-compose logs -f

# Testar a API
curl http://localhost:8080/api/health

# Testar de fora (use o IP público)
curl http://<EC2_PUBLIC_IP>:8080/api/health
```

## Dicas

- Use **Elastic IP** para manter o IP fixo mesmo após restart da EC2
- Configure **CloudWatch** para monitorar a instância
- Use **Route 53** para um domínio amigável
- Considere usar **ALB (Application Load Balancer)** para HTTPS
- Verifique o SSM Agent: `sudo systemctl status amazon-ssm-agent`
