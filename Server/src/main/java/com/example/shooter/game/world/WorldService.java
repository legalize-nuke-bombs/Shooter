package com.example.shooter.game.world;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import com.example.shooter.game.player.Player;
import com.example.shooter.game.player.PlayerRepository;
import com.example.shooter.game.player.PlayerRepresentation;
import com.example.shooter.game.player.PlayerRole;
import com.example.shooter.game.util.Vector3d;
import com.example.shooter.user.User;
import com.example.shooter.user.UserRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.List;

@Service
@Slf4j
public class WorldService {

    private final UserRepository userRepository;
    private final WorldRepository worldRepository;
    private final PlayerRepository playerRepository;

    public WorldService(UserRepository userRepository, WorldRepository worldRepository, PlayerRepository playerRepository) {
        this.userRepository = userRepository;
        this.worldRepository = worldRepository;
        this.playerRepository = playerRepository;
    }

    @Transactional
    public WorldRepresentation create(Long userId, CreateWorldRequest request) {
        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        Long now = Instant.now().getEpochSecond();

        World world = new World();
        world.setName(request.getName());
        world.setCreatedAt(now);
        world.setVisibilityPolicy(request.getVisibilityPolicy());
        world.setJoinPolicy(request.getJoinPolicy());
        world.setDescription(request.getDescription());
        world = worldRepository.save(world);

        Player player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setRole(PlayerRole.CREATOR);
        player.setMemberSince(now);
        player.setPosition(new Vector3d(0, 0, 0));
        player = playerRepository.save(player);

        log.info("user {} created new world {} player {}", userId, world.getId(), player.getId());

        return new WorldRepresentation(
                world,
                List.of(new PlayerRepresentation(player))
        );
    }
}
