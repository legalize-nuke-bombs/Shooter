package com.example.shooter.server;

import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/server")
@Validated
public class ServerController {

    private final ServerRepresentation serverRepresentation;

    public ServerController(ServerRepresentation serverRepresentation) {
        this.serverRepresentation = serverRepresentation;
    }

    @GetMapping
    public ServerRepresentation get() {
        return serverRepresentation;
    }
}
