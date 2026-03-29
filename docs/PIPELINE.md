# 🔄 Pipeline CI/CD

## Visão Geral

A pipeline possui **2 jobs** principais:

```
┌─────────────────┐      ┌─────────────────┐
│   🔨 Build      │─────▶│   🚀 Deploy     │
│   & Test        │      │   to EC2        │
└─────────────────┘      └─────────────────┘
```

## Job 1: Build & Test

**Roda em:** Todo push e PR para `main`

1. Checkout do código
2. Setup .NET 8 SDK
3. Restore das dependências
4. Build em modo Release
5. Execução dos testes (com upload dos resultados)
6. Build da imagem Docker
7. Save da imagem como artefato

## Job 2: Deploy to EC2

**Roda em:** Apenas push na `main` (após merge de PR)

1. Download do artefato Docker
2. SCP da imagem para a EC2
3. SSH na EC2 para:
   - Load da imagem Docker
   - Stop/Remove do container antigo
   - Start do novo container
   - Limpeza de imagens antigas
   - Health check

## Fluxo de Trabalho Recomendado

1. Criar uma branch a partir de `main`
2. Fazer as alterações
3. Abrir um Pull Request
4. A pipeline roda Build + Testes automaticamente
5. Após aprovação e merge, o Deploy roda automaticamente

## Secrets Necessários

| Secret        | Exemplo                              |
|---------------|--------------------------------------|
| `EC2_HOST`    | `54.123.45.67`                       |
| `EC2_USER`    | `ubuntu`                             |
| `EC2_SSH_KEY` | Conteúdo da chave `.pem` da EC2      |

### Como adicionar os Secrets

1. Vá no repositório do GitHub
2. `Settings` → `Secrets and variables` → `Actions`
3. Clique em `New repository secret`
4. Adicione cada secret listado acima

### Obtendo a chave SSH

```bash
# Copie o conteúdo da chave .pem
cat ~/.ssh/sua-chave-ec2.pem
```

Cole o conteúdo completo (incluindo `-----BEGIN RSA PRIVATE KEY-----` e `-----END RSA PRIVATE KEY-----`) como valor do secret `EC2_SSH_KEY`.

## Troubleshooting

### Build falhou
- Verifique se o .NET SDK 8.0 está configurado corretamente
- Veja os logs de teste no artefato `test-results`

### Deploy falhou
- Verifique se os secrets estão configurados corretamente
- Confirme que a EC2 tem Docker instalado
- Confirme que a porta 8080 está liberada no Security Group
- Verifique se a chave SSH está correta

### Health check falhou após deploy
- O container pode demorar alguns segundos para iniciar
- Verifique os logs: `docker logs sample-api`
