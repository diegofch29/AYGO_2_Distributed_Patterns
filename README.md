# AYGO Workshop 2 - Distributed Log Management System

This project implements a distributed log management system using a microservices architecture with load balancing, service discovery, and real-time log replication.

## Architecture Overview

The system consists of the following components:

- **Redis**: Centralized cache and service registry
- **Load Balancer**: Routes requests to available Log API instances
- **Log API Instances**: Multiple backend services for log processing
- **Frontend Client**: React application for log interaction

![Architecture Diagram](./images/Architecture.png)

## Prerequisites

Before starting the deployment, ensure you have:

- AWS Account with EC2 access
- Docker installed on all EC2 instances
- Basic knowledge of AWS EC2 and Docker

## Deployment Guide

### Step 1: Create EC2 Instances

Create the following EC2 instances on AWS:

1. **Redis Instance** (1x)
2. **Frontend + Load Balancer Instance** (1x)
3. **Log API Instances** (3x or more)

#### 1.1 Launch EC2 Instances

1. Log into your AWS Console
2. Navigate to EC2 Dashboard
3. Click "Launch Instance"

![AWS EC2 Dashboard](./images/Dashboard.png)

4. Configure each instance with the following specifications:
   - **AMI**: Amazon Linux 2 or Ubuntu 20.04 LTS
   - **Instance Type**: t2.micro (or larger based on requirements)
   - **Security Group**: Configure ports as specified below

![Instance Configuration](./images/instance_creation.png)

#### 1.2 Security Group Configuration

Configure security groups for each instance type:

**Redis Instance Security Group:**

- Port 6379 (Redis) - Source: Load Balancer and Log API instances
- Port 22 (SSH) - Source: Your IP

**Frontend + Load Balancer Instance Security Group:**

- Port 80 (HTTP) - Source: 0.0.0.0/0
- Port 8080 (Load Balancer API) - Source: 0.0.0.0/0
- Port 22 (SSH) - Source: Your IP

**Log API Instances Security Group:**

- Port 8080 (API) - Source: Load Balancer instance
- Port 22 (SSH) - Source: Your IP

![Security Group Configuration](./images/step1-security-groups.png)

#### 1.3 Note Down Instance Information

After launching, record the following for each instance:

- Instance ID
- Public IP Address
- Private IP Address

### Step 2: Setup Redis Instance

#### 2.1 Connect to Redis Instance

Connect to your Redis EC2 instance via SSH:

```bash
ssh -i your-key.pem ec2-user@<redis-public-ip>
```

#### 2.2 Install Docker

```bash
sudo yum update -y
sudo yum install docker -y
sudo service docker start
sudo usermod -a -G docker ec2-user
```

![Docker Installation](./images/Docker_install.png)

#### 2.3 Pull and Run Redis

```bash
docker pull redis:latest
docker run -d --name redis -p 6379:6379 redis:latest
```

![Redis Container](./images/Redis.png)

#### 2.4 Verify Redis Installation

```bash
docker ps
docker logs redis
```

### Step 3: Setup Frontend + Load Balancer Instance

#### 3.1 Connect to Frontend Instance

```bash
ssh -i your-key.pem ec2-user@<frontend-public-ip>
```

#### 3.2 Install Docker

```bash
sudo yum update -y
sudo yum install docker -y
sudo service docker start
sudo usermod -a -G docker ec2-user
```

#### 3.3 Run Frontend Application

Replace `<load_balancer_url_ip>` with the current instance's public IP:

```bash
docker run -d -p 80:5173 \
  -e VITE_API_BASE_URL=<load_balancer_url_ip> \
  -e VITE_API_PORT=8080 \
  diegofchb29/log_client_app_aygo_2
```

![Frontend Container](./images/Client_App.png)

#### 3.4 Run Load Balancer

Replace `<redis_ip_address>` with the Redis instance's private IP:

```bash
docker run -d -p 8080:8080 --name loadbalancer \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__Redis=<redis_ip_address>:6379 \
  diegofchb29/load_balancer_aygo_2
```

![Load Balancer Container](./images/Load_Balancer.png)

#### 3.5 Verify Deployment

```bash
docker ps
curl http://localhost:80
curl http://localhost:8080/health
```

### Step 4: Setup Log API Instances

Repeat this process for each Log API instance (typically 3 instances).

#### 4.1 Connect to Log API Instance

```bash
ssh -i your-key.pem ec2-user@<logapi-public-ip>
```

#### 4.2 Install Docker

```bash
sudo yum update -y
sudo yum install docker -y
sudo service docker start
sudo usermod -a -G docker ec2-user
```

#### 4.3 Run Log API Application

Replace the following placeholders:

- `<redis_ip_address>`: Redis instance private IP
- `<instance_id>`: Unique identifier for this instance (e.g., "instance-1", "instance-2", etc.)
- `<load_balancer_url_ip>`: Load balancer instance public IP
- `<service_ip_address>`: Current Log API instance private IP

```bash
docker run -d --name logapi-app -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__Redis=<redis_ip_address>:6379 \
  -e ServiceRegistration__ServiceName=LogApi-Dev-<instance_id> \
  -e ServiceRegistration__LoadBalancerUrl=<load_balancer_url_ip>:8080 \
  -e ServiceRegistration__ServicePort=8080 \
  -e ServiceRegistration__ServiceIpAddress=<service_ip_address> \
  diegofchb29/log_api_aygo_2
```

![Log API Container](./images/Api.png)

#### 4.4 Verify Log API Instance

```bash
docker ps
docker logs logapi-app
curl http://localhost:8080/health
```

### Step 5: System Verification and Testing

#### 5.1 Check Service Registration

Verify all Log API instances are registered with the load balancer:

```bash
curl http://<load_balancer_public_ip>:8080/api/registration/services
```

#### 5.2 Test Load Balancing

Make multiple requests to see load balancing in action:

```bash
curl http://<load_balancer_public_ip>:8080/api/log
curl http://<load_balancer_public_ip>:8080/api/log
curl http://<load_balancer_public_ip>:8080/api/log
```

#### 5.3 Access Frontend Application

Open your browser and navigate to:

```
http://<frontend_public_ip>
```

![Frontend Application](./images/Test_Client_1.png)

#### 5.4 Test Log Creation and Retrieval

1. Create a new log entry through the frontend
2. Verify the log appears in the list
3. Check that logs are distributed across instances

![Log Testing](./images/Test_Client_2.png)

## Troubleshooting

### Common Issues

#### Issue 1: Service Registration Failures

**Symptoms**: Log API instances not appearing in load balancer
**Solution**:

1. Check network connectivity between instances
2. Verify Redis connectivity
3. Check environment variables

![Troubleshooting Service Registration](./images/troubleshooting-service-registration.png)

#### Issue 2: Load Balancer Not Responding

**Symptoms**: Frontend can't connect to load balancer
**Solution**:

1. Verify load balancer container is running
2. Check port 8080 is open in security group
3. Verify Redis connection

#### Issue 3: Frontend Not Loading

**Symptoms**: Browser shows connection error
**Solution**:

1. Verify frontend container is running on port 80
2. Check security group allows HTTP traffic
3. Verify environment variables are correct

### Useful Commands

```bash
# Check all running containers
docker ps -a

# View container logs
docker logs <container-name>

# Restart a container
docker restart <container-name>

# Check container resource usage
docker stats

# Access container shell
docker exec -it <container-name> /bin/bash
```

## Cleanup

To remove all resources:

1. Stop and remove all Docker containers
2. Terminate all EC2 instances
3. Delete security groups
4. Remove any associated EBS volumes

```bash
# Stop all containers
docker stop $(docker ps -q)

# Remove all containers
docker rm $(docker ps -aq)

# Remove all images
docker rmi $(docker images -q)
```
