package com.example.shooter.game.world;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/worlds")
@Validated
public class WorldController {

    private final WorldService worldService;

    public WorldController(WorldService worldService) {
        this.worldService = worldService;
    }

    @PostMapping
    public ResponseEntity<WorldRepresentation> create(@AuthenticationPrincipal Long userId, @RequestBody @Validated CreateWorldRequest request) {
        return ResponseEntity.status(HttpStatus.CREATED).body(
                worldService.create(userId, request)
        );
    }
}
