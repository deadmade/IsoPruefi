# Troubleshooting Guide

## Common Setup Issues

### Docker Container Problems

#### Containers won't start
**Problem:** `docker compose up` fails or containers exit immediately

**Solutions:**
- Check Docker daemon is running: `docker info`
- Clear Docker cache: `docker system prune -a`
- Check logs: `docker compose logs [service-name]`
- Ensure sufficient resources: Check Docker Desktop settings (minimum 4GB RAM recommended)

#### InfluxDB connection errors
**Problem:** Cannot connect to InfluxDB or token issues

**Solutions:**
1. Verify InfluxDB is running: `docker ps | grep influxdb`
2. Check InfluxDB logs: `docker compose logs influxdb3`
3. Recreate admin token:
   ```bash
   docker exec -it influxdb influxdb3 create token --admin
   ```
4. Update `config.json` with new token in `isopruefi-docker/influx/explorer/config/`
5. Restart affected services: `docker compose restart`
6. Verify token permissions: Test with API call: `curl -H "Authorization: Token <paste-token-here>" http://localhost:8181/health`

#### Traefik routing issues
**Problem:** `*.localhost` domains not accessible

**Solutions:**
- Verify Traefik dashboard: `https://traefik.localhost:8432`
- Check container labels in `docker-compose.yml`
- Clear browser cache/cookies for localhost domains
- Try different browser or incognito mode
- On Windows: Add entries to `C:\Windows\System32\drivers\etc\hosts`:
  ```
  127.0.0.1 traefik.localhost
  127.0.0.1 frontend.localhost
  127.0.0.1 backend.localhost
  127.0.0.1 grafana.localhost
  127.0.0.1 explorer.localhost
  ```
- Check Traefik logs: `docker compose logs traefik`

#### Database connection issues
**Problem:** Services can't connect to PostgreSQL or InfluxDB

**Solutions:**
- Verify database containers are healthy: `docker compose ps`
- Check connection strings in environment files
- Test PostgreSQL connection:
  ```bash
  docker exec -it postgres psql -U Isopruefi -d Isopruefi
  ```
- Test InfluxDB connection:
  ```bash
  curl -H "Authorization: Token <paste-token-here>" http://localhost:8181/health
  ```
- Check network connectivity: `docker network ls` and `docker network inspect [network-name]`

### Arduino/PlatformIO Issues

#### PlatformIO build failures
**Problem:** Library dependencies not found or compilation errors

**Solutions:**
1. Clean and rebuild:
   ```bash
   pio run -e mkrwifi1010 -t clean
   pio lib update
   pio run -e mkrwifi1010
   ```
2. Update PlatformIO: `pio upgrade`
3. Update platform/libraries: `pio pkg update`
4. Verify `platformio.ini` configuration
5. Check VS Code opened correct folder (`isopruefi-arduino/`)
6. Clear PlatformIO cache: `pio system prune`

#### Serial connection problems
**Problem:** Cannot upload code or monitor serial output

**Solutions:**
- Check USB cable and connection (try different cable/port)
- Verify correct board selected in PlatformIO
- Close other applications using the serial port (Arduino IDE, other serial monitors)
- Reset board manually and try upload again
- Check device permissions (Linux/macOS): `sudo usermod -a -G dialout $USER` (logout/login required)
- Windows: Update/reinstall board drivers
- Try different baud rate: `pio device monitor --baud 115200`

#### MQTT connection failures
**Problem:** Arduino can't connect to MQTT broker

**Solutions:**
- Verify WiFi credentials in `secrets.h`
- Check MQTT broker is accessible:
  ```bash
  # Test MQTT broker connection
  mosquitto_pub -h your-mqtt-broker -p 1883 -t test/topic -m "test message"
  ```
- Verify network connectivity from Arduino location
- Check MQTT topic/payload format matches backend expectations
- Enable serial debugging in Arduino code to see connection attempts
- Try different MQTT broker (for testing): `mqtt://test.mosquitto.org`

### Development Environment

#### npm/Node.js issues
**Problem:** `npm install` fails or pre-commit hooks don't work

**Solutions:**
- Update Node.js to latest LTS version (18.x or 20.x recommended)
- Clear npm cache: `npm cache clean --force`
- Delete `node_modules/` and reinstall: `rm -rf node_modules package-lock.json && npm install`
- Check node/npm versions: `node --version && npm --version`
- Try yarn instead of npm: `yarn install`
- On Windows: Run as Administrator if permission issues persist
- Check proxy settings: `npm config get proxy` and `npm config get https-proxy`

#### .NET build failures
**Problem:** Backend won't compile or test failures

**Solutions:**
1. Verify .NET 9.0 SDK: `dotnet --version`
2. Restore packages: `dotnet restore`
3. Clear build cache: `dotnet clean && dotnet build`
4. Check database connection strings in `appsettings.json`
5. Verify user secrets are set:
   ```bash
   dotnet user-secrets list --project isopruefi-backend/MQTT-Receiver-Worker
   ```
6. Update NuGet packages: `dotnet add package Microsoft.AspNetCore.App`

#### Frontend build issues
**Problem:** React/TypeScript compilation errors

**Solutions:**
- Clear Vite cache: `rm -rf node_modules/.vite`
- Update dependencies: `npm update`
- Check TypeScript configuration: Verify `tsconfig.json` settings
- Restart Vite dev server: `npm run dev`
- Check for TypeScript errors: `npm run type-check`

## Performance Issues

### High resource usage
**Problem:** Docker containers consuming too much CPU/RAM

**Solutions:**
- Monitor usage: `docker stats`
- Increase Docker resource limits in Docker Desktop settings
- Check for memory leaks in application logs
- Optimize database queries and indexing
- Consider limiting container resources:
  ```yaml
  deploy:
    resources:
      limits:
        memory: 512M
        cpus: '0.5'
  ```

### Slow web interface
**Problem:** Frontend loading slowly or timeouts

**Solutions:**
- Check browser developer tools (Network tab for slow requests)
- Verify backend API response times: `docker compose logs isopruefi-backend-api-1`
- Clear browser cache and data for localhost domains
- Check for JavaScript errors in browser console
- Verify network connectivity between frontend and backend containers
- Test API endpoints directly: `curl -k https://backend.localhost/api/health`

### Database performance issues
**Problem:** Slow queries or connection timeouts

**Solutions:**
- Monitor database logs: `docker compose logs postgres` or `docker compose logs influxdb3`
- Check database indexes and query optimization
- Verify sufficient database resources
- Consider connection pooling configuration
- Monitor disk space: `docker system df`

## Data Issues

### Missing sensor data
**Problem:** Temperature readings not appearing in the system

**Solutions:**
1. Check MQTT message flow:
   ```bash
   # Check MQTT receiver logs
   docker compose logs isopruefi-mqtt-receiver-1
   
   # Test MQTT connection
   mosquitto_sub -h your-mqtt-broker -t "isopruefi/+/temperature"
   ```
2. Verify InfluxDB data:
   - Access InfluxDB Explorer: `https://explorer.localhost`
   - Check database and bucket configuration
   - Verify data retention policies
3. Check Arduino serial monitor for sensor readings and MQTT publish attempts
4. Verify SD card fallback is working (if offline)
5. Check sensor wiring and power supply
6. Validate MQTT message format matches backend expectations

### Incorrect timestamps
**Problem:** Data shows wrong time or timezone issues

**Solutions:**
- Verify Arduino RTC module battery level
- Check system timezone settings on host machine
- Synchronize time (Linux/macOS): `sudo ntpdate -s time.nist.gov`
- Verify UTC handling in backend code
- Check Docker container timezone: `docker exec -it container_name date`
- Configure timezone in containers if needed:
  ```yaml
  environment:
    - TZ=Europe/Berlin
  ```

### Data duplication
**Problem:** Duplicate sensor readings in database

**Solutions:**
- Check MQTT QoS settings and message deduplication
- Verify unique constraints in database schema
- Review SD card fallback and sync logic
- Check for network reconnection issues causing retransmission
- Implement idempotent writes in backend services

## Production Environment Issues

### Health check failures
**Problem:** Services failing health checks in production

**Solutions:**
- Check service logs: `docker compose logs [service-name]`
- Verify health check endpoints are accessible:
  ```bash
  docker exec -it container_name curl localhost:8080/health
  ```
- Adjust health check intervals and timeouts if needed
- Check service dependencies are ready before health checks start
- Verify network connectivity between containers

### SSL/TLS certificate issues
**Problem:** HTTPS certificates not working in production

**Solutions:**
- Check Traefik certificate configuration
- Verify domain name resolution
- Check certificate expiry: `openssl s_client -connect your-domain:443 -showcerts`
- Review Traefik logs for certificate acquisition errors
- Ensure proper DNS configuration for domain validation

### Load balancer issues  
**Problem:** Requests not properly distributed across service instances

**Solutions:**
- Verify Traefik configuration and service labels
- Check service discovery and registration
- Monitor load balancing metrics in Grafana
- Test individual service instances directly
- Review sticky session configuration if needed

## Getting Help

### Log Analysis Priority
Always check logs in this order:
```bash
# 1. Check overall container status
docker compose ps

# 2. Check specific service logs
docker compose logs [service-name] --tail=100 -f

# 3. Check application logs inside containers
docker exec -it [container-name] tail -f /var/log/app.log

# 4. Check system logs
journalctl -u docker (Linux)
```

### Debug Information Checklist
When reporting issues, include:
- **Error messages** and complete stack traces
- **Environment info**: OS, Docker version, available resources
- **Docker version**: `docker --version && docker compose --version`
- **Container status**: `docker compose ps`
- **System resources**: `docker stats` output
- **Recent changes** to configuration files
- **Steps to reproduce** the problem consistently

### Essential Debug Commands
```bash
# Container inspection
docker inspect [container-name]
docker compose config  # Validate compose file

# Network debugging
docker network ls
docker network inspect [network-name]

# Volume inspection
docker volume ls
docker volume inspect [volume-name]

# Resource monitoring
docker stats --no-stream
df -h  # Check disk space

# Service-specific debugging
curl -k https://service.localhost/health
netstat -tulpn | grep :PORT
```

### Common Log Locations
- **Backend API**: `docker compose logs isopruefi-backend-api-1`
- **Frontend**: Browser developer console + `docker compose logs isopruefi-frontend-1`
- **Arduino**: PlatformIO serial monitor (`pio device monitor`)
- **MQTT**: `docker compose logs isopruefi-mqtt-receiver-1`
- **Databases**: `docker compose logs postgres` or `docker compose logs influxdb3`
- **System logs**: 
  - Linux: `journalctl -u docker`
  - macOS: Console.app or `log show --predicate 'process == "Docker"'`
  - Windows: Event Viewer (Application logs)

For additional help beyond this guide:
- Check the [contributing guide](contributing.md) for development workflow issues
- Review [API documentation](api-reference.md) for backend integration problems  
- Create an issue in the GitHub repository with debug information