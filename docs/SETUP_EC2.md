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
   - **Key pair:** Crie ou selecione um par de chaves
   - **Security Group:** Veja seção abaixo

### Ou via AWS CLI
```bash
aws ec2 run-instances \
  --image-id ami-0c55b159cbfafe1f0 \
  --instance-type t2.micro \
  --key-name sua-chave \
  --security-group-ids sg-xxxxxxxx \
  --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=sample-api-server}]'
```

## 2. Security Group

Libere as seguintes portas:

| Tipo   | Protocolo | Porta | Origem        | Descrição         |
|--------|-----------|-------|---------------|-------------------|
| SSH    | TCP       | 22    | Seu IP / GitHub Actions | Acesso SSH    |
| Custom | TCP       | 8080  | 0.0.0.0/0     | API               |

> ⚠️ Em produção, restrinja o SSH apenas para IPs conhecidos ou use um bastion host.

## 3. Instalar Docker na EC2

### Amazon Linux 2023
```bash
# Conectar na EC2
ssh -i sua-chave.pem ec2-user@<EC2_PUBLIC_IP>

# Instalar Docker
sudo yum update -y
sudo yum install -y docker
sudo systemctl start docker
sudo systemctl enable docker

# Adicionar usuário ao grupo docker
sudo usermod -aG docker ec2-user

# Sair e reconectar para aplicar as permissões
exit
```

### Ubuntu 22.04
```bash
# Conectar na EC2
ssh -i sua-chave.pem ubuntu@<EC2_PUBLIC_IP>

# Instalar Docker
sudo apt-get update
sudo apt-get install -y docker.io
sudo systemctl start docker
sudo systemctl enable docker

# Adicionar usuário ao grupo docker
sudo usermod -aG docker ubuntu

# Sair e reconectar para aplicar as permissões
exit
```

## 4. Verificar Docker

```bash
# Reconectar
ssh -i sua-chave.pem ec2-user@<EC2_PUBLIC_IP>

# Testar Docker
docker --version
docker run hello-world
```

## 5. Configurar Secrets no GitHub

Agora configure os secrets no repositório:

```
EC2_HOST = <IP_PÚBLICO_DA_EC2>
EC2_USER = ec2-user          # Amazon Linux
EC2_USER = ubuntu             # Ubuntu
EC2_SSH_KEY = <conteúdo da chave .pem>
```

## 6. Testar a Conexão

```bash
# Do seu computador local
ssh -i sua-chave.pem ec2-user@<EC2_PUBLIC_IP> "echo 'Conexão OK!'"
```

## 7. Após o Deploy

```bash
# Verificar se o container está rodando
docker ps

# Ver logs
docker logs sample-api

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
