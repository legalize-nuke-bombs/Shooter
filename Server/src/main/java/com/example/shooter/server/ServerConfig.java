package com.example.shooter.server;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class ServerConfig {

    private final ServerRepresentation serverRepresentation;

    public ServerConfig(@Value("${server.name}") String name, @Value("${server.major}") int major, @Value("${server.minor}") int minor, @Value("${server.patch}") int patch) {
        this.serverRepresentation = new ServerRepresentation(
                name,
                major,
                minor,
                patch
        );
    }

    @Bean
    public ServerRepresentation serverRepresentation() {
        return serverRepresentation;
    }
}
