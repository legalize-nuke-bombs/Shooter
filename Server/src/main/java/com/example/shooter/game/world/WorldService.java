package com.example.shooter.game.world;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import com.example.shooter.game.player.Player;
import com.example.shooter.game.player.PlayerRepository;
import com.example.shooter.game.player.PlayerRepresentation;
import com.example.shooter.game.player.PlayerRole;
import com.example.shooter.user.User;
import com.example.shooter.user.UserRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.*;
import java.util.stream.Collectors;

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
        world.setAccessedAt(now);
        world.setVisibilityPolicy(request.getVisibilityPolicy());
        world.setJoinPolicy(request.getJoinPolicy());
        world.setDescription(request.getDescription());
        world = worldRepository.save(world);

        Player player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setRole(PlayerRole.CREATOR);
        player.setMemberSince(now);
        player = playerRepository.save(player);

        log.info("user {} created new world {} player {}", userId, world.getId(), player.getId());

        return new WorldRepresentation(
                world,
                List.of(new PlayerRepresentation(player))
        );
    }

    public List<WorldRepresentation> get(Long userId, PlayerRole playerRole, WorldOrder order, Integer page, Integer size) {
        Set<UUID> requiredWorldIds;
        WorldVisibilityPolicy requiredVisibilityPolicy;
        WorldJoinPolicy requiredJoinPolicy;
        if (playerRole == null) {
            requiredWorldIds = null;
            requiredVisibilityPolicy = WorldVisibilityPolicy.PUBLIC;
            requiredJoinPolicy = WorldJoinPolicy.EVERYONE;
        }
        else {
            requiredWorldIds = playerRepository.findAllByUserIdAndPlayerRoleWithWorlds(userId, playerRole)
                    .stream()
                    .map(Player::getWorld)
                    .map(World::getId)
                    .collect(Collectors.toSet());
            requiredVisibilityPolicy = null;
            requiredJoinPolicy = null;
        }

        Pageable pageable = PageRequest.of(page, size);

        List<World> worlds;
        switch (order) {
            case NAME -> {
                worlds = worldRepository.findByWorldIdsAndVisibilityPolicyNameOrder(requiredWorldIds, requiredVisibilityPolicy, requiredJoinPolicy, pageable);
            }
            case CREATED_AT -> {
                worlds = worldRepository.findByWorldIdsAndVisibilityPolicyCreatedAtOrder(requiredWorldIds, requiredVisibilityPolicy, requiredJoinPolicy, pageable);
            }
            case ACCESSED_AT -> {
                worlds = worldRepository.findByWorldIdsAndVisibilityPolicyAccessedAtOrder(requiredWorldIds, requiredVisibilityPolicy, requiredJoinPolicy, pageable);
            }
            default -> {
                log.warn("user {} sent unexpected world order {}", userId, order);
                worlds = List.of();
            }
        }

        Set<UUID> worldIds = worlds.stream().map(World::getId).collect(Collectors.toSet());

        List<Player> players = playerRepository.findAllByWorldIdsWithWorldsAndUsers(worldIds);

        Map<UUID, WorldRepresentation> resultMap = new LinkedHashMap<>();
        for (World w : worlds) {
            resultMap.put(w.getId(), new WorldRepresentation(w, new ArrayList<>()));
        }
        for (Player p : players) {
            resultMap.get(p.getWorld().getId()).getPlayers().add(new PlayerRepresentation(p));
        }

        return resultMap.values().stream().toList();
    }
}
