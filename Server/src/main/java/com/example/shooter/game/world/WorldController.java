package com.example.shooter.game.world;

import com.example.shooter.game.player.PlayerRole;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.*;

import java.util.List;

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

    @GetMapping
    public List<WorldRepresentation> get(
            @AuthenticationPrincipal Long userId,
            @RequestParam(required = false) PlayerRole playerRole,
            @RequestParam(defaultValue = "ACCESSED_AT") WorldOrder order,
            @RequestParam(defaultValue = "0") @Min(0) Integer page,
            @RequestParam(defaultValue = "100") @Min(0) @Max(100) Integer size
            ) {
        return worldService.get(userId, playerRole, order, page, size);
    }
}
